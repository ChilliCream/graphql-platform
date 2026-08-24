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
        var (result, frames) = await SendToListeningSocketAsync("hello", cancellationToken);

        // assert
        Assert.Equal(ClaudePeerSendResult.Ok, result);
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
        var (result, frames) = await SendToListeningSocketAsync("authenticated", cancellationToken);

        // assert
        Assert.Equal(ClaudePeerSendResult.Ok, result);
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
        var result = await _client.SendAsync(Pid, SessionId, "hello", cancellationToken);

        // assert
        Assert.Equal(ClaudePeerSendResult.Unsupported, result);
    }

    [Fact]
    public async Task SendAsync_Should_ReturnErrorWithoutDowngrading_When_KeyGenerationDoesNotMatch()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await WriteRegistryAsync(protocol: 1, cancellationToken);
        await WriteKeyAsync("different-generation", cancellationToken);

        // act
        var result = await _client.SendAsync(Pid, SessionId, "hello", cancellationToken);

        // assert
        Assert.Equal(ClaudePeerSendResult.Error, result);
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
        var result = await _client.SendAsync(Pid, SessionId, "hello", cancellationToken);

        // assert
        Assert.Equal(ClaudePeerSendResult.EndpointGone, result);
    }

    [Fact]
    public async Task SendAsync_Should_ReturnEndpointGone_When_RegistrySessionDoesNotMatch()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await WriteRegistryAsync(protocol: 1, cancellationToken);

        // act
        var result = await _client.SendAsync(Pid, "different-session", "hello", cancellationToken);

        // assert
        Assert.Equal(ClaudePeerSendResult.EndpointGone, result);
    }

    private async Task<(ClaudePeerSendResult Result, IReadOnlyList<JsonDocument> Frames)>
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

        var result = await sendTask;
        var lines = Encoding.UTF8.GetString(buffer.ToArray())
            .Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var frames = lines.Select(line => JsonDocument.Parse(line)).ToArray();

        return (result, frames);
    }

    private Socket CreateListener()
    {
        var listener = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        listener.Bind(new UnixDomainSocketEndPoint(_socketPath));
        listener.Listen(1);
        return listener;
    }

    private Task WriteRegistryAsync(int protocol, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(new
        {
            pid = Pid,
            sessionId = SessionId,
            kind = "interactive",
            procStart = ProcStart,
            peerProtocol = protocol,
            messagingSocketPath = _socketPath
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
