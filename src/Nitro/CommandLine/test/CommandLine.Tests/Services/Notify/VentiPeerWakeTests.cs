using System.Text;
using System.Text.Json;
using ChilliCream.Nitro.CommandLine.Services.Hook;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Notify;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using Microsoft.Extensions.Time.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Drives a Venti session from its hooks to a mail wake: the Venti hook handler registers the
/// session against a temporary peer registry, and the real wake dispatcher, ping executor, mail
/// nudge, and peer client deliver to a real Unix domain socket, or a named pipe on Windows,
/// listed in that registry.
/// </summary>
public sealed class VentiPeerWakeTests : IDisposable
{
    private const string LeaderToken = "leader-token";
    private const string SessionId = "0123456789abcdef0123456789abcdef";
    private const int Pid = 4242;
    private const long ProcStart = 1767960000000;
    private const string PeerToken = "fedcba9876543210fedcba9876543210";

    private readonly DirectoryInfo _tempRoot;
    private readonly string _workspaceRoot;
    private readonly string _workspaceDirectory;
    private readonly string _peerDirectory;
    private readonly FakeTimeProvider _timeProvider;
    private readonly AgentDatabase _database;
    private readonly AgentStore _agentStore;
    private readonly AgentDeliveryLedger _ledger;
    private readonly MailStore _mail;
    private readonly MailWakeBatchStore _batches;
    private readonly MailWakeDaemonLeaderStore _leaderStore;
    private readonly ClaudePeerClient _peerClient;
    private readonly ActorWakeDispatcher _dispatcher;
    private readonly ClaudeHookHandler _hooks;

    public VentiPeerWakeTests()
    {
        // A short root keeps the socket path within the platform's socket address limit.
        _tempRoot = Directory.CreateTempSubdirectory("vw");
        _workspaceRoot = Path.Combine(_tempRoot.FullName, "repo");
        _workspaceDirectory = AgentWorkspace.GetDirectory(_workspaceRoot);
        Directory.CreateDirectory(_workspaceDirectory);
        _peerDirectory = Path.Combine(_tempRoot.FullName, "peers");
        Directory.CreateDirectory(_peerDirectory);

        var fileSystem = new TestFileSystem(_workspaceRoot);
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero));
        _database = new AgentDatabase();
        _agentStore = new AgentStore(fileSystem, _timeProvider, _database);
        _ledger = new AgentDeliveryLedger(fileSystem, _database);
        _mail = new MailStore(fileSystem, _timeProvider, _database, _agentStore);
        _batches = new MailWakeBatchStore(fileSystem, _database);
        _leaderStore = new MailWakeDaemonLeaderStore(fileSystem, _database);
        _peerClient = new ClaudePeerClient(Path.Combine(_tempRoot.FullName, "claude"), _peerDirectory);

        var leases = new PingLeaseStore(fileSystem, _database);
        var executor = new PingSessionExecutor(
            _mail,
            _ledger,
            new NeverCompletingCodexQueueClient(),
            _peerClient,
            _agentStore,
            leases,
            _timeProvider,
            new FakeOpencodeServerClient());
        _dispatcher = new ActorWakeDispatcher(
            _batches,
            _agentStore,
            new SessionGateCoordinator(new AgentPingGateStore(fileSystem, _database), leases),
            executor,
            _mail,
            _timeProvider);
        _hooks = new ClaudeHookHandler(
            fileSystem,
            _timeProvider,
            _agentStore,
            _ledger,
            _mail,
            AgentSessionHarness.Venti,
            new VentiPeerRegistryReader(_peerDirectory));
    }

    public void Dispose() => _tempRoot.Delete(recursive: true);

    [Fact]
    public async Task DispatchAsync_Should_SendTheDigestWithoutRecordingDelivery_When_AnInteractiveVentiSessionHasUnreadMail()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        using var endpoint = VentiPeerTestEndpoint.Listen(_peerDirectory);
        await WriteRegistryAsync(endpoint.Address, cancellationToken);
        await WriteKeyAsync(cancellationToken);
        var actor = await StartVentiSessionAsync(cancellationToken);
        await SendMailAsync(actor, MailWakePolicy.Enqueue, cancellationToken);
        var frames = ReadFramesAsync(endpoint, cancellationToken);

        // act
        var receipt = await _dispatcher.DispatchAsync(actor, LeaderToken, Deadline(), cancellationToken);

        // assert
        var target = Assert.Single(receipt!.Targets);
        Assert.Equal((MailWakeTargetStatus.Failed, "Unconfirmed"), (target.Status, target.LastError));
        (await frames).MatchInlineSnapshot(
            """
            [
              "auth token=fedcba9876543210fedcba9876543210",
              "user session_id=0123456789abcdef0123456789abcdef role=user"
            ]
            """);
    }

    [Fact]
    public async Task DispatchAsync_Should_FailWithInvalidAuthWithoutConnecting_When_TheVentiKeyIsMissing()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        using var endpoint = VentiPeerTestEndpoint.Listen(_peerDirectory);
        await WriteRegistryAsync(endpoint.Address, cancellationToken);
        var actor = await StartVentiSessionAsync(cancellationToken);
        await SendMailAsync(actor, MailWakePolicy.Enqueue, cancellationToken);

        // act
        var receipt = await _dispatcher.DispatchAsync(actor, LeaderToken, Deadline(), cancellationToken);

        // assert
        var target = Assert.Single(receipt!.Targets);
        Assert.Equal((MailWakeTargetStatus.Failed, "InvalidAuth"), (target.Status, target.LastError));
        Assert.False(await endpoint.HasConnectionAsync());
    }

    [Fact]
    public async Task DispatchAsync_Should_SkipAsUnreachable_When_TheVentiSessionAdvertisesNoEndpoint()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await StartVentiSessionAsync(cancellationToken);
        await SendMailAsync(actor, MailWakePolicy.Enqueue, cancellationToken);

        // act
        var receipt = await _dispatcher.DispatchAsync(actor, LeaderToken, Deadline(), cancellationToken);

        // assert
        var target = Assert.Single(receipt!.Targets);
        Assert.Equal((MailWakeTargetStatus.Skipped, "unreachable"), (target.Status, target.LastError));
    }

    [Fact]
    public async Task NudgeAsync_Should_DeliverTheDigestToTheVentiSocket_When_AnInteractiveSessionHasUnreadMail()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        using var endpoint = VentiPeerTestEndpoint.Listen(_peerDirectory);
        await WriteRegistryAsync(endpoint.Address, cancellationToken);
        await WriteKeyAsync(cancellationToken);
        var actor = await StartVentiSessionAsync(cancellationToken);
        await SendMailAsync(actor, MailWakePolicy.Skip, cancellationToken);
        var nudge = new MailNudge(
            _agentStore, _mail, _ledger, _peerClient, new NeverCompletingCodexQueueClient(), _timeProvider);
        var frames = ReadFramesAsync(endpoint, cancellationToken);

        // act
        await nudge.NudgeAsync([actor], cancellationToken);

        // assert
        (await frames).MatchInlineSnapshot(
            """
            [
              "auth token=fedcba9876543210fedcba9876543210",
              "user session_id=0123456789abcdef0123456789abcdef role=user"
            ]
            """);
    }

    private DateTimeOffset Deadline() => _timeProvider.GetUtcNow() + WakeDispatchPolicy.BatchDeadline;

    private async Task InitializeWorkspaceAsync(CancellationToken cancellationToken)
    {
        await using (await _database.InitializeAsync(_workspaceDirectory, cancellationToken))
        {
        }

        await _leaderStore.TryAcquireAsync(
            LeaderToken, _timeProvider.GetUtcNow(), TimeSpan.FromDays(1), cancellationToken);
    }

    private async Task<string> StartVentiSessionAsync(CancellationToken cancellationToken)
    {
        await _hooks.HandleSessionStartAsync(
            new ClaudeHookPayload { SessionId = SessionId, Cwd = _workspaceRoot },
            skipSessionFileLookup: false,
            cancellationToken);

        return (await _agentStore.FindBySessionAsync(AgentSessionHarness.Venti, SessionId, cancellationToken))!.Name;
    }

    private async Task SendMailAsync(string actor, MailWakePolicy wakePolicy, CancellationToken cancellationToken)
    {
        await using (var connection = await _database.ConnectAsync(_workspaceDirectory, cancellationToken))
        {
            await using var command = connection.CreateCommand();
            command.CommandText =
                """
                INSERT INTO agents (name, registered_at, started_at, last_seen_at)
                VALUES ('pascal', @now, @now, @now);
                """;
            command.Parameters.AddWithValue("@now", _timeProvider.GetUtcNow());
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await _mail.SendMessageAsync(
            new MailMessageCreation
            {
                Sender = "pascal",
                Subject = "status",
                Body = "check",
                To = [actor],
                WakePolicy = wakePolicy
            },
            cancellationToken);
    }

    private Task WriteRegistryAsync(string endpoint, CancellationToken cancellationToken)
        => File.WriteAllTextAsync(
            Path.Combine(_peerDirectory, SessionId + ".json"),
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

    private Task WriteKeyAsync(CancellationToken cancellationToken)
        => VentiPeerTestEndpoint.WriteKeyFileAsync(
            Path.Combine(_peerDirectory, $"{Pid}.0a1b2c3d.key"),
            JsonSerializer.Serialize(new { procStart = ProcStart, peerToken = PeerToken }),
            UnixFileMode.UserRead | UnixFileMode.UserWrite,
            cancellationToken);

    /// <summary>
    /// Accepts one peer connection and summarizes each JSON line it sends, leaving the digest
    /// text out.
    /// </summary>
    private static async Task<IReadOnlyList<string>> ReadFramesAsync(
        VentiPeerTestEndpoint endpoint,
        CancellationToken cancellationToken)
    {
        await using var stream = await endpoint.AcceptAsync(cancellationToken);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var lines = (await reader.ReadToEndAsync(cancellationToken))
            .Split('\n', StringSplitOptions.RemoveEmptyEntries);

        return lines
            .Select(line => JsonDocument.Parse(line).RootElement)
            .Select(frame => frame.GetProperty("type").GetString() == "auth"
                ? $"auth token={frame.GetProperty("token").GetString()}"
                : $"user session_id={frame.GetProperty("session_id").GetString()} "
                    + $"role={frame.GetProperty("message").GetProperty("role").GetString()}")
            .ToList();
    }
}
