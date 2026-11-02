using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using ChilliCream.Nitro.CommandLine.Services.Notify;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Tests <see cref="ClaudePeerClient"/> routing to Venti's peer registry, with registry and key
/// files shaped as Venti writes them and a real Unix domain socket, or a named pipe on Windows,
/// standing in for the endpoint.
/// </summary>
public sealed class VentiPeerClientTests : IDisposable
{
    private const int Pid = 4242;
    private const string SessionId = "0123456789abcdef0123456789abcdef";
    private const long ProcStart = 1767960000000;
    private const string PeerToken = "fedcba9876543210fedcba9876543210";
    private const UnixFileMode OwnerOnly = UnixFileMode.UserRead | UnixFileMode.UserWrite;

    // Longer than the client's transport budget, so only a send without one runs past it.
    private static readonly TimeSpan s_completionBound = TimeSpan.FromSeconds(10);

    // Far larger than a socket or pipe buffer, so writing it stalls until the peer reads.
    private static readonly string s_unbufferedMessage = new('x', 8 * 1024 * 1024);

    private readonly DirectoryInfo _tempRoot;
    private readonly string _claudeDirectory;
    private readonly string _peerDirectory;
    private readonly ClaudePeerClient _client;

    public VentiPeerClientTests()
    {
        // A short root keeps the socket path within the platform's socket address limit.
        _tempRoot = Directory.CreateTempSubdirectory("vp");
        _claudeDirectory = Path.Combine(_tempRoot.FullName, "claude");
        _peerDirectory = Path.Combine(_tempRoot.FullName, "peers");
        Directory.CreateDirectory(_claudeDirectory);
        Directory.CreateDirectory(_peerDirectory);
        _client = new ClaudePeerClient(_claudeDirectory, _peerDirectory);
    }

    public void Dispose() => _tempRoot.Delete(recursive: true);

    [Fact]
    public async Task SendAsync_Should_AuthenticateThenDeliverTheMessage_When_TheVentiKeyMatchesTheProcess()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var endpoint = VentiPeerTestEndpoint.Listen(_peerDirectory);
        await WriteRegistryAsync(_peerDirectory, endpoint.Address, cancellationToken);
        await WriteKeyAsync("0a1b2c3d", ProcStart, cancellationToken);

        // act
        var (outcome, frames) = await SendAndReadAsync(endpoint, "hello venti", cancellationToken);

        // assert
        Assert.Equal(ClaudePeerSendOutcome.Unconfirmed, outcome);
        frames.MatchInlineSnapshot(
            """
            {"type":"auth","token":"fedcba9876543210fedcba9876543210"}
            {"type":"user","session_id":"0123456789abcdef0123456789abcdef","message":{"role":"user","content":"hello venti"}}

            """);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("stale")]
    [InlineData("insecure")]
    [InlineData("ambiguous")]
    [InlineData("malformed-token")]
    public async Task SendAsync_Should_ReturnInvalidAuthWithoutConnecting_When_TheVentiKeyIsNotUsable(string keyState)
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var endpoint = VentiPeerTestEndpoint.Listen(_peerDirectory);
        await WriteRegistryAsync(_peerDirectory, endpoint.Address, cancellationToken);
        await WriteKeyStateAsync(keyState, cancellationToken);

        // act
        var outcome = await _client.SendAsync(AgentSessionHarness.Venti, SessionId, "hello", cancellationToken);

        // assert
        Assert.Equal(ClaudePeerSendOutcome.InvalidAuth, outcome);
        Assert.False(await endpoint.HasConnectionAsync());
    }

    [Fact]
    public async Task SendAsync_Should_ReturnUnconfirmed_When_AVentiEndpointAcceptsTheToken()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var endpoint = VentiPeerTestEndpoint.Listen(_peerDirectory);
        await WriteRegistryAsync(_peerDirectory, endpoint.Address, cancellationToken);
        await WriteKeyAsync("0a1b2c3d", ProcStart, cancellationToken);
        var serving = ServeLikeVentiAsync(endpoint, PeerToken, TimeSpan.Zero, cancellationToken);

        // act
        var outcome = await _client.SendAsync(AgentSessionHarness.Venti, SessionId, "hello venti", cancellationToken);

        // assert
        Assert.Equal(
            (ClaudePeerSendOutcome.Unconfirmed, "hello venti"),
            (outcome, string.Join(", ", await serving)));
    }

    [Fact]
    public async Task SendAsync_Should_ReturnInvalidAuth_When_AVentiEndpointRejectsTheToken()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var endpoint = VentiPeerTestEndpoint.Listen(_peerDirectory);
        await WriteRegistryAsync(_peerDirectory, endpoint.Address, cancellationToken);
        await WriteKeyAsync("0a1b2c3d", ProcStart, cancellationToken);
        var serving = ServeLikeVentiAsync(
            endpoint, "0123456789abcdef0123456789abcdef", TimeSpan.Zero, cancellationToken);

        // act
        var outcome = await _client.SendAsync(AgentSessionHarness.Venti, SessionId, "hello venti", cancellationToken);

        // assert
        Assert.Equal(
            (ClaudePeerSendOutcome.InvalidAuth, 0),
            (outcome, (await serving).Count));
    }

    [Fact]
    public async Task SendAsync_Should_ReturnUnconfirmed_When_AVentiEndpointRejectsTheTokenAfterTheRejectionWindow()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var endpoint = VentiPeerTestEndpoint.Listen(_peerDirectory);
        await WriteRegistryAsync(_peerDirectory, endpoint.Address, cancellationToken);
        await WriteKeyAsync("0a1b2c3d", ProcStart, cancellationToken);
        var serving = ServeLikeVentiAsync(
            endpoint, "0123456789abcdef0123456789abcdef", TimeSpan.FromMilliseconds(750), cancellationToken);

        // act
        var outcome = await _client.SendAsync(AgentSessionHarness.Venti, SessionId, "hello venti", cancellationToken);

        // assert
        Assert.Equal(
            (ClaudePeerSendOutcome.Unconfirmed, 0),
            (outcome, (await serving).Count));
    }

    [Fact]
    [SupportedOSPlatform("macos")]
    public async Task SendAsync_Should_ReturnInvalidAuthWithoutConnecting_When_AMacOSKeyAclGrantsEveryoneAccess()
    {
        Assert.SkipUnless(OperatingSystem.IsMacOS(), "Extended file access control lists are checked on macOS.");

        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var endpoint = VentiPeerTestEndpoint.Listen(_peerDirectory);
        await WriteRegistryAsync(_peerDirectory, endpoint.Address, cancellationToken);
        await WriteKeyAsync("0a1b2c3d", ProcStart, cancellationToken);
        await AddMacOSAclEntryAsync("everyone allow read", cancellationToken);

        // act
        var outcome = await _client.SendAsync(AgentSessionHarness.Venti, SessionId, "hello", cancellationToken);

        // assert
        Assert.Equal(
            (ClaudePeerSendOutcome.InvalidAuth, UnixFileMode.UserRead | UnixFileMode.UserWrite, false),
            (outcome, File.GetUnixFileMode(KeyPath("0a1b2c3d")), await endpoint.HasConnectionAsync()));
    }

    [Fact]
    [SupportedOSPlatform("macos")]
    public async Task SendAsync_Should_DeliverTheMessage_When_AMacOSKeyAclGrantsOnlyTheCurrentUser()
    {
        Assert.SkipUnless(OperatingSystem.IsMacOS(), "Extended file access control lists are checked on macOS.");

        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var endpoint = VentiPeerTestEndpoint.Listen(_peerDirectory);
        await WriteRegistryAsync(_peerDirectory, endpoint.Address, cancellationToken);
        await WriteKeyAsync("0a1b2c3d", ProcStart, cancellationToken);
        await AddMacOSAclEntryAsync(Environment.UserName + " allow read,write", cancellationToken);
        var serving = ServeLikeVentiAsync(endpoint, PeerToken, TimeSpan.Zero, cancellationToken);

        // act
        var outcome = await _client.SendAsync(AgentSessionHarness.Venti, SessionId, "hello venti", cancellationToken);

        // assert
        Assert.Equal(
            (ClaudePeerSendOutcome.Unconfirmed, "hello venti"),
            (outcome, string.Join(", ", await serving)));
    }

    [Fact]
    [SupportedOSPlatform("windows")]
    public async Task SendAsync_Should_ReturnInvalidAuthWithoutConnecting_When_TheWindowsKeyGrantsEveryoneAccess()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Named pipes and file access control lists are Windows only.");

        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var endpoint = VentiPeerTestEndpoint.Listen(_peerDirectory);
        await WriteRegistryAsync(_peerDirectory, endpoint.Address, cancellationToken);
        await WriteKeyAsync("0a1b2c3d", ProcStart, cancellationToken);
        var keyFile = new FileInfo(KeyPath("0a1b2c3d"));
        var security = keyFile.GetAccessControl();
        security.AddAccessRule(
            new FileSystemAccessRule(
                new SecurityIdentifier(WellKnownSidType.WorldSid, null),
                FileSystemRights.Read,
                AccessControlType.Allow));
        keyFile.SetAccessControl(security);

        // act
        var outcome = await _client.SendAsync(AgentSessionHarness.Venti, SessionId, "hello", cancellationToken);

        // assert
        Assert.Equal((ClaudePeerSendOutcome.InvalidAuth, false), (outcome, await endpoint.HasConnectionAsync()));
    }

    [Fact]
    public async Task SendAsync_Should_CompleteWithoutDelivery_When_TheVentiEndpointIsStale()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        string staleAddress;

        using (var endpoint = VentiPeerTestEndpoint.Listen(_peerDirectory))
        {
            staleAddress = endpoint.Address;
        }

        await WriteRegistryAsync(_peerDirectory, staleAddress, cancellationToken);
        await WriteKeyAsync("0a1b2c3d", ProcStart, cancellationToken);

        // act
        var outcome = await _client.SendAsync(AgentSessionHarness.Venti, SessionId, "hello", cancellationToken)
            .WaitAsync(s_completionBound, cancellationToken);

        // assert
        // A Unix socket refuses the connection, while a Windows client waits for the missing pipe.
        Assert.Equal(
            OperatingSystem.IsWindows() ? ClaudePeerSendOutcome.Timeout : ClaudePeerSendOutcome.EndpointGone,
            outcome);
    }

    [Fact]
    public async Task SendAsync_Should_ReturnTimeout_When_AVentiEndpointStopsReadingTheMessage()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var endpoint = VentiPeerTestEndpoint.Listen(_peerDirectory);
        await WriteRegistryAsync(_peerDirectory, endpoint.Address, cancellationToken);
        await WriteKeyAsync("0a1b2c3d", ProcStart, cancellationToken);

        // act
        var outcome = await _client
            .SendAsync(AgentSessionHarness.Venti, SessionId, s_unbufferedMessage, cancellationToken)
            .WaitAsync(s_completionBound, cancellationToken);

        // assert
        Assert.Equal(ClaudePeerSendOutcome.Timeout, outcome);
    }

    [Fact]
    public async Task SendAsync_Should_PropagateCancellation_When_TheCallerCancelsWhileAVentiEndpointStopsReading()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var caller = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        using var endpoint = VentiPeerTestEndpoint.Listen(_peerDirectory);
        await WriteRegistryAsync(_peerDirectory, endpoint.Address, cancellationToken);
        await WriteKeyAsync("0a1b2c3d", ProcStart, cancellationToken);
        var sending = _client.SendAsync(AgentSessionHarness.Venti, SessionId, s_unbufferedMessage, caller.Token);
        await using var connection = await endpoint.AcceptAsync(cancellationToken);

        // act
        await caller.CancelAsync();

        // assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => sending.WaitAsync(s_completionBound, cancellationToken));
    }

    [Fact]
    public async Task StaysOpenAsync_Should_ReturnTrue_When_TheTransportBudgetExpiresAfterTheMessageWasWritten()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var endpoint = VentiPeerTestEndpoint.Listen(_peerDirectory);
        await using var client = await endpoint.ConnectAsync(cancellationToken);
        await using var connection = await endpoint.AcceptAsync(cancellationToken);
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        await budget.CancelAsync();

        // act
        var staysOpen = await ClaudePeerClient.StaysOpenAsync(client, budget.Token, cancellationToken);

        // assert
        Assert.True(staysOpen);
    }

    [Fact]
    public async Task StaysOpenAsync_Should_PropagateCancellation_When_TheCallerCancelsWhileTheEndpointStaysOpen()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var endpoint = VentiPeerTestEndpoint.Listen(_peerDirectory);
        await using var client = await endpoint.ConnectAsync(cancellationToken);
        await using var connection = await endpoint.AcceptAsync(cancellationToken);
        using var caller = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(caller.Token);
        await caller.CancelAsync();

        // act
        var observing = ClaudePeerClient.StaysOpenAsync(client, budget.Token, caller.Token);

        // assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => observing);
    }

    [Fact]
    public async Task SendAsync_Should_ReturnEndpointGone_When_OnlyTheClaudeRegistryListsTheSession()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var endpoint = VentiPeerTestEndpoint.Listen(_peerDirectory);
        await WriteRegistryAsync(_claudeDirectory, endpoint.Address, cancellationToken);

        // act
        var outcome = await _client.SendAsync(AgentSessionHarness.Venti, SessionId, "hello", cancellationToken);

        // assert
        Assert.Equal(ClaudePeerSendOutcome.EndpointGone, outcome);
        Assert.False(await endpoint.HasConnectionAsync());
    }

    [Fact]
    public async Task SendAsync_Should_ReturnEndpointGone_When_OnlyTheVentiRegistryListsAClaudeSession()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var endpoint = VentiPeerTestEndpoint.Listen(_peerDirectory);
        await WriteRegistryAsync(_peerDirectory, endpoint.Address, cancellationToken);
        await WriteKeyAsync("0a1b2c3d", ProcStart, cancellationToken);

        // act
        var outcome = await _client.SendAsync(AgentSessionHarness.ClaudeCode, SessionId, "hello", cancellationToken);

        // assert
        Assert.Equal(ClaudePeerSendOutcome.EndpointGone, outcome);
        Assert.False(await endpoint.HasConnectionAsync());
    }

    [Fact]
    public async Task SendAsync_Should_ReturnUnsupported_When_TheHarnessHasNoPeerEndpoints()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var endpoint = VentiPeerTestEndpoint.Listen(_peerDirectory);
        await WriteRegistryAsync(_peerDirectory, endpoint.Address, cancellationToken);
        await WriteKeyAsync("0a1b2c3d", ProcStart, cancellationToken);

        // act
        var outcome = await _client.SendAsync(AgentSessionHarness.Codex, SessionId, "hello", cancellationToken);

        // assert
        Assert.Equal(ClaudePeerSendOutcome.Unsupported, outcome);
    }

    private async Task WriteKeyStateAsync(string keyState, CancellationToken cancellationToken)
    {
        switch (keyState)
        {
            case "missing":
                break;

            case "stale":
                await WriteKeyAsync("0a1b2c3d", ProcStart - 1, cancellationToken);
                break;

            case "insecure":
                if (OperatingSystem.IsWindows())
                {
                    Assert.Skip("Windows key files carry no Unix file mode.");
                }

                await WriteKeyJsonAsync(
                    "0a1b2c3d",
                    JsonSerializer.Serialize(new { procStart = ProcStart, peerToken = PeerToken }),
                    OwnerOnly | UnixFileMode.GroupRead,
                    cancellationToken);
                break;

            case "ambiguous":
                await WriteKeyAsync("0a1b2c3d", ProcStart, cancellationToken);
                await WriteKeyAsync("4e5f6a7b", ProcStart, cancellationToken);
                break;

            case "malformed-token":
                await WriteKeyJsonAsync(
                    "0a1b2c3d",
                    JsonSerializer.Serialize(new { procStart = ProcStart, peerToken = "not-a-token" }),
                    OwnerOnly,
                    cancellationToken);
                break;
        }
    }

    private static Task WriteRegistryAsync(string directory, string endpoint, CancellationToken cancellationToken)
        => File.WriteAllTextAsync(
            Path.Combine(directory, SessionId + ".json"),
            JsonSerializer.Serialize(
                new
                {
                    sessionId = SessionId,
                    pid = Pid,
                    kind = "interactive",
                    messagingSocketPath = endpoint,
                    procStart = ProcStart,
                    peerProtocol = 1
                }),
            cancellationToken);

    private Task WriteKeyAsync(string suffix, long procStart, CancellationToken cancellationToken)
        => WriteKeyJsonAsync(
            suffix,
            JsonSerializer.Serialize(new { procStart, peerToken = PeerToken }),
            OwnerOnly,
            cancellationToken);

    private string KeyPath(string suffix) => Path.Combine(_peerDirectory, $"{Pid}.{suffix}.key");

    /// <summary>
    /// Adds <paramref name="entry"/> to the access control list of the key file, leaving its mode bits as they are.
    /// </summary>
    private async Task AddMacOSAclEntryAsync(string entry, CancellationToken cancellationToken)
    {
        using var chmod = System.Diagnostics.Process.Start("/bin/chmod", ["+a", entry, KeyPath("0a1b2c3d")]);
        await chmod.WaitForExitAsync(cancellationToken);
        Assert.Equal(0, chmod.ExitCode);
    }

    private Task WriteKeyJsonAsync(
        string suffix,
        string json,
        UnixFileMode mode,
        CancellationToken cancellationToken)
        => VentiPeerTestEndpoint.WriteKeyFileAsync(KeyPath(suffix), json, mode, cancellationToken);

    /// <summary>
    /// Serves one connection as a Venti peer endpoint does: closes it
    /// <paramref name="rejectionDelay"/> after a first line that is not an auth line with
    /// <paramref name="token"/>, otherwise returns the content of every user message line until
    /// the peer hangs up.
    /// </summary>
    private static async Task<IReadOnlyList<string>> ServeLikeVentiAsync(
        VentiPeerTestEndpoint endpoint,
        string token,
        TimeSpan rejectionDelay,
        CancellationToken cancellationToken)
    {
        await using var stream = await endpoint.AcceptAsync(cancellationToken);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var messages = new List<string>();

        if (await reader.ReadLineAsync(cancellationToken) is not { } authLine
            || JsonDocument.Parse(authLine).RootElement.GetProperty("token").GetString() != token)
        {
            await Task.Delay(rejectionDelay, cancellationToken);
            return messages;
        }

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            messages.Add(
                JsonDocument.Parse(line).RootElement.GetProperty("message").GetProperty("content").GetString()!);
        }

        return messages;
    }

    private async Task<(ClaudePeerSendOutcome Outcome, string Frames)> SendAndReadAsync(
        VentiPeerTestEndpoint endpoint,
        string message,
        CancellationToken cancellationToken)
    {
        var sendTask = _client.SendAsync(AgentSessionHarness.Venti, SessionId, message, cancellationToken);
        await using var stream = await endpoint.AcceptAsync(cancellationToken);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var frames = await reader.ReadToEndAsync(cancellationToken);

        return (await sendTask, frames);
    }
}
