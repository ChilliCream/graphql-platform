using System.Buffers;
using System.IO.Pipes;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.Json;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Services.Notify;

/// <summary>
/// Sends messages using Claude Code's peer protocol 1 to the local endpoint a Claude Code or
/// Venti session advertises in its harness's registry directory. Claude Code authenticates
/// with a key matching the target process when one is present, and always on Windows. Venti
/// always requires one that only the user can access, and a Venti send it does not reject is
/// <see cref="ClaudePeerSendOutcome.Unconfirmed"/>, because the protocol sends no acknowledgement.
/// A Venti send that does not finish writing the message within a short time budget is
/// <see cref="ClaudePeerSendOutcome.Timeout"/>.
/// </summary>
internal sealed class ClaudePeerClient : IClaudePeerClient
{
    internal const int SupportedProtocol = 1;
    internal const string OldestCharacterizedVersion = "2.1.226";
    internal const string LastVerifiedVersion = "2.1.241";

    private const int MaxRegistryBytes = 64 * 1024;
    private const int MaxKeyBytes = 4 * 1024;
    private const string WindowsPipePrefix = @"\\.\pipe\";
    private const int GenericAll = 0x10000000;
    private const int GenericWrite = 0x40000000;
    private const uint GenericRead = 0x80000000;

    // A Venti endpoint closes the connection right after reading a wrong token and gives no
    // other answer, so a close within this window is a rejection; later ones go unobserved.
    private static readonly TimeSpan s_rejectionWindow = TimeSpan.FromMilliseconds(250);

    // A Windows client waits for a missing pipe instead of being refused, and a peer that stops
    // reading stalls the write, so a Venti send that has not finished by then is abandoned.
    private static readonly TimeSpan s_ventiTransportBudget = TimeSpan.FromSeconds(2);

    private readonly string _claudeSessionDirectory;
    private readonly string _ventiPeerDirectory;

    public ClaudePeerClient()
        : this(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".claude", "sessions"),
            VentiPeerRegistryReader.DefaultDirectory)
    {
    }

    internal ClaudePeerClient(string claudeSessionDirectory, string ventiPeerDirectory)
    {
        _claudeSessionDirectory = claudeSessionDirectory;
        _ventiPeerDirectory = ventiPeerDirectory;
    }

    public async Task<ClaudePeerSendOutcome> SendAsync(
        string harness,
        string sessionId,
        string message,
        CancellationToken cancellationToken)
    {
        string registryDirectory;
        bool requiresKey;
        bool authenticatesStrictly;

        switch (harness)
        {
            case AgentSessionHarness.ClaudeCode:
                registryDirectory = _claudeSessionDirectory;
                requiresKey = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
                authenticatesStrictly = false;
                break;

            case AgentSessionHarness.Venti:
                registryDirectory = _ventiPeerDirectory;
                requiresKey = true;
                authenticatesStrictly = true;
                break;

            default:
                return ClaudePeerSendOutcome.Unsupported;
        }

        try
        {
            // Reads current registry files to find the session's advertised endpoint.
            var registryJson = await FindRegistryAsync(registryDirectory, sessionId, cancellationToken);

            if (registryJson is null)
            {
                return ClaudePeerSendOutcome.EndpointGone;
            }

            using var registryDocument = JsonDocument.Parse(registryJson);
            var root = registryDocument.RootElement;

            if (!TryReadRegistry(root, sessionId, out var pid, out var endpoint, out var procStart))
            {
                return ClaudePeerSendOutcome.EndpointGone;
            }

            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("peerProtocol", out var protocolElement)
                || protocolElement.ValueKind != JsonValueKind.Number
                || !protocolElement.TryGetInt32(out var protocol)
                || protocol != SupportedProtocol)
            {
                return ClaudePeerSendOutcome.Unsupported;
            }

            var key = await ResolveKeyAsync(
                registryDirectory, pid, procStart, authenticatesStrictly, cancellationToken);

            if (!key.Success || (requiresKey && key.Token is null))
            {
                return ClaudePeerSendOutcome.InvalidAuth;
            }

            var payload = BuildPayload(sessionId, message, key.Token);

            if (!authenticatesStrictly)
            {
                await SendPayloadAsync(
                    endpoint, payload, observesRejection: false, cancellationToken, cancellationToken);
                return ClaudePeerSendOutcome.Ok;
            }

            using var budget = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            budget.CancelAfter(s_ventiTransportBudget);

            try
            {
                return await SendPayloadAsync(
                        endpoint, payload, observesRejection: true, budget.Token, cancellationToken)
                    ? ClaudePeerSendOutcome.Unconfirmed
                    : ClaudePeerSendOutcome.InvalidAuth;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return ClaudePeerSendOutcome.Timeout;
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (FileNotFoundException)
        {
            return ClaudePeerSendOutcome.EndpointGone;
        }
        catch (DirectoryNotFoundException)
        {
            return ClaudePeerSendOutcome.EndpointGone;
        }
        catch (SocketException exception) when (exception.SocketErrorCode == SocketError.AccessDenied)
        {
            return ClaudePeerSendOutcome.AccessDenied;
        }
        catch (IOException exception) when (exception.InnerException is SocketException
        { SocketErrorCode: SocketError.AccessDenied })
        {
            return ClaudePeerSendOutcome.AccessDenied;
        }
        catch (SocketException exception) when (IsGone(exception.SocketErrorCode))
        {
            return ClaudePeerSendOutcome.EndpointGone;
        }
        catch (IOException exception) when (exception.InnerException is SocketException socketException
            && IsGone(socketException.SocketErrorCode))
        {
            return ClaudePeerSendOutcome.EndpointGone;
        }
        catch (SocketException exception)
        {
            return ClaudePeerSendOutcome.TransportError(exception.SocketErrorCode.ToString());
        }
        catch (JsonException exception)
        {
            return ClaudePeerSendOutcome.TransportError(exception.GetType().Name);
        }
        catch (IOException exception)
        {
            return ClaudePeerSendOutcome.TransportError(exception.GetType().Name);
        }
        catch (UnauthorizedAccessException exception)
        {
            return ClaudePeerSendOutcome.TransportError(exception.GetType().Name);
        }
        catch (ArgumentException exception)
        {
            return ClaudePeerSendOutcome.TransportError(exception.GetType().Name);
        }
    }

    /// <summary>
    /// Returns the registry JSON matching <paramref name="sessionId"/>, or null if none exists.
    /// Throws a JSON parsing error when no match is found and a registry file contains malformed JSON.
    /// </summary>
    private static async Task<string?> FindRegistryAsync(
        string registryDirectory,
        string sessionId,
        CancellationToken cancellationToken)
    {
        if (!Directory.Exists(registryDirectory))
        {
            return null;
        }

        JsonException? malformed = null;

        foreach (var path in Directory.EnumerateFiles(
            registryDirectory, "*.json", SearchOption.TopDirectoryOnly))
        {
            var json = await ReadBoundedTextAsync(path, MaxRegistryBytes, cancellationToken);

            if (json is null)
            {
                continue;
            }

            try
            {
                using var document = JsonDocument.Parse(json);

                if (document.RootElement.ValueKind == JsonValueKind.Object
                    && document.RootElement.TryGetProperty("sessionId", out var id)
                    && id.ValueKind == JsonValueKind.String
                    && id.GetString() == sessionId)
                {
                    return json;
                }
            }
            catch (JsonException exception)
            {
                // A matching file takes precedence over a JSON parsing error in another file.
                malformed ??= exception;
            }
        }

        return malformed is null ? null : throw malformed;
    }

    private static bool TryReadRegistry(
        JsonElement root,
        string expectedSessionId,
        out int pid,
        out string endpoint,
        out string procStart)
    {
        pid = 0;
        endpoint = string.Empty;
        procStart = string.Empty;

        if (root.ValueKind != JsonValueKind.Object
            || !root.TryGetProperty("pid", out var pidElement)
            || pidElement.ValueKind != JsonValueKind.Number
            || !pidElement.TryGetInt32(out pid)
            || pid <= 0
            || !root.TryGetProperty("sessionId", out var sessionIdElement)
            || sessionIdElement.ValueKind != JsonValueKind.String
            || sessionIdElement.GetString() != expectedSessionId
            || !root.TryGetProperty("kind", out var kindElement)
            || kindElement.ValueKind != JsonValueKind.String
            || kindElement.GetString() != "interactive"
            || !root.TryGetProperty("messagingSocketPath", out var endpointElement)
            || endpointElement.ValueKind != JsonValueKind.String
            || string.IsNullOrEmpty(endpointElement.GetString())
            || !root.TryGetProperty("procStart", out var procStartElement))
        {
            return false;
        }

        endpoint = endpointElement.GetString()!;
        procStart = procStartElement.ValueKind switch
        {
            JsonValueKind.String => procStartElement.GetString() ?? string.Empty,
            JsonValueKind.Number => procStartElement.GetRawText(),
            _ => string.Empty
        };

        return procStart.Length > 0;
    }

    private static async Task<KeyResolution> ResolveKeyAsync(
        string registryDirectory,
        int pid,
        string procStart,
        bool requiresOwnerOnly,
        CancellationToken cancellationToken)
    {
        if (!Directory.Exists(registryDirectory))
        {
            return KeyResolution.NoKey;
        }

        var keyPaths = Directory
            .EnumerateFiles(registryDirectory, $"{pid}.*.key", SearchOption.TopDirectoryOnly)
            .Order(StringComparer.Ordinal)
            .ToArray();

        if (keyPaths.Length == 0)
        {
            return KeyResolution.NoKey;
        }

        string? matchedToken = null;

        foreach (var keyPath in keyPaths)
        {
            if (!IsSecureKeyFile(keyPath, requiresOwnerOnly))
            {
                return KeyResolution.Invalid;
            }

            var keyJson = await ReadBoundedTextAsync(keyPath, MaxKeyBytes, cancellationToken);

            if (keyJson is null)
            {
                return KeyResolution.Invalid;
            }

            using var keyDocument = JsonDocument.Parse(keyJson);
            var root = keyDocument.RootElement;

            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("procStart", out var procStartElement)
                || ReadScalar(procStartElement) != procStart)
            {
                continue;
            }

            if (matchedToken is not null
                || !root.TryGetProperty("peerToken", out var tokenElement)
                || tokenElement.ValueKind != JsonValueKind.String
                || tokenElement.GetString() is not { Length: 32 } token
                || !token.All(Uri.IsHexDigit))
            {
                return KeyResolution.Invalid;
            }

            matchedToken = token;
        }

        // Existing keys without a matching process incarnation are invalid authentication.
        return matchedToken is null
            ? KeyResolution.Invalid
            : new KeyResolution(true, matchedToken);
    }

    /// <summary>
    /// Whether the key file is a regular file only the user can read or write. With
    /// <paramref name="requiresOwnerOnly"/>, it must also be owned by the current user, and on
    /// Windows and macOS its access control list may grant no other account access.
    /// </summary>
    private static bool IsSecureKeyFile(string path, bool requiresOwnerOnly)
    {
        var info = new FileInfo(path);

        if (!info.Exists || info.LinkTarget is not null)
        {
            return false;
        }

        if (OperatingSystem.IsWindows())
        {
            return !requiresOwnerOnly || IsOwnerOnlyOnWindows(info);
        }

        var mode = File.GetUnixFileMode(path);
        const UnixFileMode disallowed =
            UnixFileMode.GroupRead
            | UnixFileMode.GroupWrite
            | UnixFileMode.GroupExecute
            | UnixFileMode.OtherRead
            | UnixFileMode.OtherWrite
            | UnixFileMode.OtherExecute;

        if ((mode & disallowed) != 0)
        {
            return false;
        }

        if (!requiresOwnerOnly)
        {
            return true;
        }

        // A Linux access control list's mask shows in the group bits checked above, while a
        // macOS one can grant access the mode bits do not show.
        return UnixFileOwner.IsCurrentUser(path)
            && (!OperatingSystem.IsMacOS() || MacOSFileAcl.GrantsOnlyCurrentUser(path));
    }

    /// <summary>
    /// Whether the file is owned by the current user, the local system, or the administrators,
    /// and grants no other account access to its contents or its access control list.
    /// </summary>
    [SupportedOSPlatform("windows")]
    private static bool IsOwnerOnlyOnWindows(FileInfo file)
    {
        const FileSystemRights exposingRights =
            FileSystemRights.ReadData
            | FileSystemRights.WriteData
            | FileSystemRights.AppendData
            | FileSystemRights.ChangePermissions
            | FileSystemRights.TakeOwnership
            | (FileSystemRights)GenericAll
            | (FileSystemRights)GenericWrite
            | unchecked((FileSystemRights)GenericRead);

        using var identity = WindowsIdentity.GetCurrent();
        SecurityIdentifier?[] trusted =
        [
            identity.User,
            new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null),
            new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null),
            // OWNER RIGHTS grants only what the owner, already checked, holds.
            new SecurityIdentifier("S-1-3-4")
        ];

        var security = file.GetAccessControl();

        if (security.GetOwner(typeof(SecurityIdentifier)) is not SecurityIdentifier owner
            || !trusted.Contains(owner))
        {
            return false;
        }

        foreach (FileSystemAccessRule rule in security.GetAccessRules(true, true, typeof(SecurityIdentifier)))
        {
            if (rule.AccessControlType == AccessControlType.Allow
                && (rule.FileSystemRights & exposingRights) != 0
                && !trusted.Contains(rule.IdentityReference as SecurityIdentifier))
            {
                return false;
            }
        }

        return true;
    }

    private static string? ReadScalar(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Number => element.GetRawText(),
        _ => null
    };

    private static async Task<string?> ReadBoundedTextAsync(
        string path,
        int maxBytes,
        CancellationToken cancellationToken)
    {
        var info = new FileInfo(path);

        if (!info.Exists)
        {
            return null;
        }

        if (info.Length > maxBytes)
        {
            throw new IOException($"File exceeds the {maxBytes}-byte protocol limit.");
        }

        return await File.ReadAllTextAsync(path, cancellationToken);
    }

    private static byte[] BuildPayload(string sessionId, string message, string? peerToken)
    {
        var buffer = new ArrayBufferWriter<byte>(message.Length + 256);

        if (peerToken is not null)
        {
            using (var writer = new Utf8JsonWriter(buffer))
            {
                writer.WriteStartObject();
                writer.WriteString("type", "auth");
                writer.WriteString("token", peerToken);
                writer.WriteEndObject();
            }

            AppendNewline(buffer);
        }

        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            writer.WriteString("type", "user");
            writer.WriteString("session_id", sessionId);
            writer.WritePropertyName("message");
            writer.WriteStartObject();
            writer.WriteString("role", "user");
            writer.WriteString("content", message);
            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        AppendNewline(buffer);
        return buffer.WrittenSpan.ToArray();
    }

    private static void AppendNewline(ArrayBufferWriter<byte> buffer)
    {
        buffer.GetSpan(1)[0] = (byte)'\n';
        buffer.Advance(1);
    }

    /// <summary>
    /// Writes <paramref name="payload"/> to the endpoint within <paramref name="transportToken"/>. With
    /// <paramref name="observesRejection"/>, returns false when the endpoint closes the
    /// connection within the rejection window; otherwise returns true, which confirms no delivery.
    /// </summary>
    private static async Task<bool> SendPayloadAsync(
        string endpoint,
        ReadOnlyMemory<byte> payload,
        bool observesRejection,
        CancellationToken transportToken,
        CancellationToken callerToken)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            if (!endpoint.StartsWith(WindowsPipePrefix, StringComparison.OrdinalIgnoreCase)
                || endpoint.Length == WindowsPipePrefix.Length)
            {
                throw new ArgumentException("Unsupported Claude peer endpoint.", nameof(endpoint));
            }

            var pipeName = endpoint[WindowsPipePrefix.Length..];
            await using var pipe = new NamedPipeClientStream(
                ".",
                pipeName,
                observesRejection ? PipeDirection.InOut : PipeDirection.Out,
                PipeOptions.Asynchronous);
            await pipe.ConnectAsync(transportToken);
            await pipe.WriteAsync(payload, transportToken);
            await pipe.FlushAsync(transportToken);

            return !observesRejection || await StaysOpenAsync(pipe, transportToken, callerToken);
        }

        if (!Path.IsPathFullyQualified(endpoint))
        {
            throw new ArgumentException("Unsupported Claude peer endpoint.", nameof(endpoint));
        }

        using var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        await socket.ConnectAsync(new UnixDomainSocketEndPoint(endpoint), transportToken);
        await using var stream = new NetworkStream(socket, ownsSocket: false);
        await stream.WriteAsync(payload, transportToken);
        await stream.FlushAsync(transportToken);

        if (observesRejection)
        {
            return await StaysOpenAsync(stream, transportToken, callerToken);
        }

        socket.Shutdown(SocketShutdown.Send);
        return true;
    }

    /// <summary>
    /// Returns true when the peer neither closes the connection nor writes to it within the
    /// rejection window or before <paramref name="transportToken"/> is cancelled, and throws when
    /// <paramref name="callerToken"/> is cancelled.
    /// </summary>
    internal static async Task<bool> StaysOpenAsync(
        Stream stream,
        CancellationToken transportToken,
        CancellationToken callerToken)
    {
        using var window = CancellationTokenSource.CreateLinkedTokenSource(transportToken);
        window.CancelAfter(s_rejectionWindow);

        try
        {
            // A Venti endpoint never writes, so any completed read means it closed the connection.
            _ = await stream.ReadAtLeastAsync(new byte[1], 1, throwOnEndOfStream: false, window.Token);
            return false;
        }
        catch (OperationCanceledException) when (!callerToken.IsCancellationRequested)
        {
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (SocketException)
        {
            return false;
        }
    }

    private static bool IsGone(SocketError error)
        => error is SocketError.AddressNotAvailable
            or SocketError.ConnectionAborted
            or SocketError.ConnectionRefused
            or SocketError.ConnectionReset
            or SocketError.HostNotFound
            or SocketError.NotConnected;

    private readonly record struct KeyResolution(bool Success, string? Token)
    {
        public static KeyResolution NoKey { get; } = new(true, null);
        public static KeyResolution Invalid { get; } = new(false, null);
    }
}
