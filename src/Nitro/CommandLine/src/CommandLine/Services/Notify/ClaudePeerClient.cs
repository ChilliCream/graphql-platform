using System.Buffers;
using System.IO.Pipes;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace ChilliCream.Nitro.CommandLine.Services.Notify;

/// <summary>
/// Protocol-1 client for Claude Code's local cross-session inbox. The
/// endpoint and compatibility fields are always read from Claude Code's
/// own per-process registry row. Authentication is feature-detected from
/// the matching key file, never inferred from the installed CLI version.
/// </summary>
internal sealed class ClaudePeerClient : IClaudePeerClient
{
    internal const int SupportedProtocol = 1;
    internal const string OldestCharacterizedVersion = "2.1.226";
    internal const string LastVerifiedVersion = "2.1.241";

    private const int MaxRegistryBytes = 64 * 1024;
    private const int MaxKeyBytes = 4 * 1024;
    private const string WindowsPipePrefix = @"\\.\pipe\";

    private readonly string _sessionDirectory;

    public ClaudePeerClient()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".claude",
            "sessions"))
    {
    }

    internal ClaudePeerClient(string sessionDirectory)
    {
        _sessionDirectory = sessionDirectory;
    }

    public async Task<ClaudePeerSendResult> SendAsync(
        int pid,
        string sessionId,
        string message,
        CancellationToken cancellationToken)
    {
        try
        {
            var registryPath = Path.Combine(_sessionDirectory, $"{pid}.json");
            var registryJson = await ReadBoundedTextAsync(
                registryPath, MaxRegistryBytes, cancellationToken);

            if (registryJson is null)
            {
                return ClaudePeerSendResult.EndpointGone;
            }

            using var registryDocument = JsonDocument.Parse(registryJson);
            var root = registryDocument.RootElement;

            if (!TryReadRegistry(root, pid, sessionId, out var endpoint, out var procStart))
            {
                return ClaudePeerSendResult.EndpointGone;
            }

            if (!root.TryGetProperty("peerProtocol", out var protocolElement)
                || !protocolElement.TryGetInt32(out var protocol)
                || protocol != SupportedProtocol)
            {
                return ClaudePeerSendResult.Unsupported;
            }

            var key = await ResolveKeyAsync(pid, procStart, cancellationToken);

            if (!key.Success)
            {
                return ClaudePeerSendResult.Error;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && key.Token is null)
            {
                // Claude Code requires inbox authentication on Windows. A
                // missing key means this endpoint cannot be spoken safely.
                return ClaudePeerSendResult.Error;
            }

            var payload = BuildPayload(sessionId, message, key.Token);
            await SendPayloadAsync(endpoint, payload, cancellationToken);

            return ClaudePeerSendResult.Ok;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (FileNotFoundException)
        {
            return ClaudePeerSendResult.EndpointGone;
        }
        catch (DirectoryNotFoundException)
        {
            return ClaudePeerSendResult.EndpointGone;
        }
        catch (SocketException exception) when (IsGone(exception.SocketErrorCode))
        {
            return ClaudePeerSendResult.EndpointGone;
        }
        catch (IOException exception) when (exception.InnerException is SocketException socketException
            && IsGone(socketException.SocketErrorCode))
        {
            return ClaudePeerSendResult.EndpointGone;
        }
        catch (JsonException)
        {
            return ClaudePeerSendResult.Error;
        }
        catch (IOException)
        {
            return ClaudePeerSendResult.Error;
        }
        catch (UnauthorizedAccessException)
        {
            return ClaudePeerSendResult.Error;
        }
        catch (ArgumentException)
        {
            return ClaudePeerSendResult.Error;
        }
    }

    private static bool TryReadRegistry(
        JsonElement root,
        int expectedPid,
        string expectedSessionId,
        out string endpoint,
        out string procStart)
    {
        endpoint = string.Empty;
        procStart = string.Empty;

        if (!root.TryGetProperty("pid", out var pidElement)
            || !pidElement.TryGetInt32(out var pid)
            || pid != expectedPid
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

    private async Task<KeyResolution> ResolveKeyAsync(
        int pid,
        string procStart,
        CancellationToken cancellationToken)
    {
        if (!Directory.Exists(_sessionDirectory))
        {
            return KeyResolution.NoKey;
        }

        var keyPaths = Directory
            .EnumerateFiles(_sessionDirectory, $"{pid}.*.key", SearchOption.TopDirectoryOnly)
            .Order(StringComparer.Ordinal)
            .ToArray();

        if (keyPaths.Length == 0)
        {
            return KeyResolution.NoKey;
        }

        string? matchedToken = null;

        foreach (var keyPath in keyPaths)
        {
            if (!IsSecureKeyFile(keyPath))
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

            if (!root.TryGetProperty("procStart", out var procStartElement)
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

        // A key exists for this pid, but none belongs to the current Claude
        // process incarnation. Never downgrade that case to unauthenticated.
        return matchedToken is null
            ? KeyResolution.Invalid
            : new KeyResolution(true, matchedToken);
    }

    private static bool IsSecureKeyFile(string path)
    {
        var info = new FileInfo(path);

        if (!info.Exists || info.LinkTarget is not null)
        {
            return false;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return true;
        }

        var mode = File.GetUnixFileMode(path);
        const UnixFileMode disallowed =
            UnixFileMode.GroupRead
            | UnixFileMode.GroupWrite
            | UnixFileMode.GroupExecute
            | UnixFileMode.OtherRead
            | UnixFileMode.OtherWrite
            | UnixFileMode.OtherExecute;

        return (mode & disallowed) == 0;
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

    private static async Task SendPayloadAsync(
        string endpoint,
        ReadOnlyMemory<byte> payload,
        CancellationToken cancellationToken)
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
                ".", pipeName, PipeDirection.Out, PipeOptions.Asynchronous);
            await pipe.ConnectAsync(cancellationToken);
            await pipe.WriteAsync(payload, cancellationToken);
            await pipe.FlushAsync(cancellationToken);
            return;
        }

        if (!Path.IsPathFullyQualified(endpoint))
        {
            throw new ArgumentException("Unsupported Claude peer endpoint.", nameof(endpoint));
        }

        using var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        await socket.ConnectAsync(new UnixDomainSocketEndPoint(endpoint), cancellationToken);
        await using var stream = new NetworkStream(socket, ownsSocket: false);
        await stream.WriteAsync(payload, cancellationToken);
        await stream.FlushAsync(cancellationToken);
        socket.Shutdown(SocketShutdown.Send);
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
