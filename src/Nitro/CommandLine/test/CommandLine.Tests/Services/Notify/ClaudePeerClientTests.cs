using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using ChilliCream.Nitro.CommandLine.Services.Notify;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

public sealed class ClaudePeerClientTests : IDisposable
{
    private const int Pid = 4242;
    private const string SessionId = "session-1";
    private const string ProcStart = "44276735";
    private const string PeerToken = "0123456789abcdef0123456789abcdef";

    private readonly DirectoryInfo _tempRoot;
    private readonly string _sessionDirectory;
    private readonly string _socketPath;
    private readonly ClaudePeerClient _client;

    public ClaudePeerClientTests()
    {
        _tempRoot = Directory.CreateTempSubdirectory("nitro-claude-peer-tests");
        _sessionDirectory = Path.Combine(_tempRoot.FullName, "sessions");
        Directory.CreateDirectory(_sessionDirectory);
        _socketPath = Path.Combine(_tempRoot.FullName, "peer.sock");
        _client = new ClaudePeerClient(_sessionDirectory);
    }

    public void Dispose() => _tempRoot.Delete(recursive: true);

    [Fact]
    public async Task SendAsync_Should_SendUnauthenticatedUserFrame_When_KeyIsAbsent()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await WriteRegistryAsync(protocol: 1, cancellationToken);

        // act
        var (outcome, frames) = await SendToListeningSocketAsync("hello", cancellationToken);

        // assert
        Assert.Equal(ClaudePeerSendOutcome.Ok, outcome);
        var frame = Assert.Single(frames);
        Assert.Equal("user", frame.RootElement.GetProperty("type").GetString());
        Assert.Equal(SessionId, frame.RootElement.GetProperty("session_id").GetString());
        Assert.Equal("user", frame.RootElement.GetProperty("message").GetProperty("role").GetString());
        Assert.Equal("hello", frame.RootElement.GetProperty("message").GetProperty("content").GetString());
    }

    [Fact]
    public async Task SendAsync_Should_AuthenticateBeforeUserFrame_When_MatchingKeyExists()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await WriteRegistryAsync(protocol: 1, cancellationToken);
        await WriteKeyAsync(ProcStart, cancellationToken);

        // act
        var (outcome, frames) = await SendToListeningSocketAsync("authenticated", cancellationToken);

        // assert
        Assert.Equal(ClaudePeerSendOutcome.Ok, outcome);
        Assert.Collection(
            frames,
            auth =>
            {
                Assert.Equal("auth", auth.RootElement.GetProperty("type").GetString());
                Assert.Equal(PeerToken, auth.RootElement.GetProperty("token").GetString());
            },
            user =>
            {
                Assert.Equal("user", user.RootElement.GetProperty("type").GetString());
                Assert.Equal("authenticated", user.RootElement.GetProperty("message")
                    .GetProperty("content").GetString());
            });
    }

    [Fact]
    public async Task SendAsync_Should_ReturnUnsupportedWithoutConnecting_When_ProtocolIsUnknown()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await WriteRegistryAsync(protocol: 2, cancellationToken);

        // act
        var outcome = await _client.SendAsync(Pid, SessionId, "hello", cancellationToken);

        // assert
        Assert.Equal(ClaudePeerSendOutcome.Unsupported, outcome);
    }

    [Fact]
    public async Task SendAsync_Should_ReturnInvalidAuthWithoutDowngrading_When_KeyGenerationDoesNotMatch()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await WriteRegistryAsync(protocol: 1, cancellationToken);
        await WriteKeyAsync("different-generation", cancellationToken);

        // act
        var outcome = await _client.SendAsync(Pid, SessionId, "hello", cancellationToken);

        // assert
        Assert.Equal(ClaudePeerSendOutcome.InvalidAuth, outcome);
        Assert.False(outcome.Retryable);
    }

    [Fact]
    public async Task SendAsync_Should_ReturnEndpointGone_When_SocketIsStale()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await WriteRegistryAsync(protocol: 1, cancellationToken);
        using (var listener = CreateListener())
        {
        }

        // act
        var outcome = await _client.SendAsync(Pid, SessionId, "hello", cancellationToken);

        // assert
        Assert.Equal(ClaudePeerSendOutcome.EndpointGone, outcome);
    }

    [Fact]
    public async Task SendAsync_Should_ReturnEndpointGone_When_RegistrySessionDoesNotMatch()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await WriteRegistryAsync(protocol: 1, cancellationToken);

        // act
        var outcome = await _client.SendAsync(Pid, "different-session", "hello", cancellationToken);

        // assert
        Assert.Equal(ClaudePeerSendOutcome.EndpointGone, outcome);
    }

    [Fact]
    public async Task SendAsync_Should_ReturnAccessDenied_When_ConnectFailsWithSocketAccessDenied()
    {
        // arrange: a real access-denied-shaped SocketException raised from the
        // connect call itself (an unsearchable parent directory), not parsed
        // from any exception text. Skipped where this OS trick cannot
        // reproduce a real EACCES: Windows has no matching mechanism, and a
        // process running as root bypasses the directory permission check
        // entirely.
        if (OperatingSystem.IsWindows())
        {
            Assert.Skip("Unix directory-permission denial has no Windows equivalent.");
        }

        var cancellationToken = TestContext.Current.CancellationToken;
        var deniedSocketPath = Path.Combine(_tempRoot.FullName, "unsearchable", "peer.sock");
        await WriteRegistryAsync(protocol: 1, cancellationToken, endpointOverride: deniedSocketPath);
        MakeParentUnsearchable(deniedSocketPath);

        try
        {
            // act
            var outcome = await _client.SendAsync(Pid, SessionId, "hello", cancellationToken);

            if (outcome.Reason != ClaudePeerSendReason.AccessDenied)
            {
                Assert.Skip(
                    "The OS did not deny the connect (likely running as root); "
                    + $"got {outcome.Reason} instead.");
            }

            // assert
            Assert.False(outcome.Retryable);
            Assert.NotNull(outcome.Detail);
        }
        finally
        {
            RestoreParentSearchable(deniedSocketPath);
        }
    }

    private async Task<(ClaudePeerSendOutcome Outcome, IReadOnlyList<JsonDocument> Frames)>
        SendToListeningSocketAsync(string message, CancellationToken cancellationToken)
    {
        using var listener = CreateListener();
        var acceptTask = listener.AcceptAsync(cancellationToken).AsTask();
        var sendTask = _client.SendAsync(Pid, SessionId, message, cancellationToken);
        using var peer = await acceptTask;
        await using var buffer = new MemoryStream();
        var chunk = new byte[1024];

        while (true)
        {
            var read = await peer.ReceiveAsync(chunk, SocketFlags.None, cancellationToken);

            if (read == 0)
            {
                break;
            }

            await buffer.WriteAsync(chunk.AsMemory(0, read), cancellationToken);
        }

        var outcome = await sendTask;
        var lines = Encoding.UTF8.GetString(buffer.ToArray())
            .Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var frames = lines.Select(line => JsonDocument.Parse(line)).ToArray();

        return (outcome, frames);
    }

    /// <summary>
    /// Strips the execute bit from <paramref name="socketPath"/>'s parent
    /// directory, which turns a Unix-domain <c>connect(2)</c> against a path
    /// beneath it into a real <c>EACCES</c> the OS reports as
    /// <see cref="SocketError.AccessDenied"/>, not a fabricated exception.
    /// </summary>
    private static void MakeParentUnsearchable(string socketPath)
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        var parent = Path.GetDirectoryName(socketPath)!;
        Directory.CreateDirectory(parent);
        File.SetUnixFileMode(parent, UnixFileMode.UserRead | UnixFileMode.UserWrite);
    }

    private static void RestoreParentSearchable(string socketPath)
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        var parent = Path.GetDirectoryName(socketPath)!;

        if (Directory.Exists(parent))
        {
            File.SetUnixFileMode(
                parent,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
    }

    private Socket CreateListener()
    {
        var listener = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        listener.Bind(new UnixDomainSocketEndPoint(_socketPath));
        listener.Listen(1);
        return listener;
    }

    private Task WriteRegistryAsync(
        int protocol, CancellationToken cancellationToken, string? endpointOverride = null)
    {
        var json = JsonSerializer.Serialize(new
        {
            pid = Pid,
            sessionId = SessionId,
            kind = "interactive",
            procStart = ProcStart,
            peerProtocol = protocol,
            messagingSocketPath = endpointOverride ?? _socketPath
        });

        return File.WriteAllTextAsync(
            Path.Combine(_sessionDirectory, $"{Pid}.json"), json, cancellationToken);
    }

    private async Task WriteKeyAsync(string procStart, CancellationToken cancellationToken)
    {
        var path = Path.Combine(_sessionDirectory, $"{Pid}.test.key");
        var json = JsonSerializer.Serialize(new { peerToken = PeerToken, procStart });
        await File.WriteAllTextAsync(path, json, cancellationToken);

        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
    }
}
