using ChilliCream.Nitro.CommandLine.Commands.Agent;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Notify;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tests.Commands;
using ChilliCream.Nitro.CommandLine.Tests.Hook;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Time.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Exercises <see cref="ClaudeRoundTripProbe"/> directly against a real
/// workspace database with a fixed ancestor resolver, the same composition
/// <see cref="AgentSessionRegistryTests"/> uses for <c>SelfClaimAsync</c>:
/// <c>doctor --probe claude</c>'s own ancestor-walk depends on the REAL
/// process tree it runs under, which a command-level test cannot control
/// (see <c>ClaimSessionCommandTests</c>), so the round trip itself is
/// verified here instead of through the CLI.
/// </summary>
public sealed class ClaudeRoundTripProbeTests : IDisposable
{
    private const string Host = "host-probe-tests";

    private readonly DirectoryInfo _tempRoot;
    private readonly string _workspaceDirectory;
    private readonly TestFileSystem _fileSystem;
    private readonly FakeTimeProvider _timeProvider;
    private readonly AgentDatabase _database;
    private readonly AgentRegistry _agentRegistry;
    private readonly MailStore _mail;
    private readonly SessionDeliveryLedger _ledger;
    private readonly PingLeaseStore _leases;
    private readonly FakeClaudePeerClient _peerClient;

    public ClaudeRoundTripProbeTests()
    {
        _tempRoot = Directory.CreateTempSubdirectory("nitro-claude-round-trip-probe-tests");
        _workspaceDirectory = AgentWorkspace.GetDirectory(_tempRoot.FullName);
        Directory.CreateDirectory(_workspaceDirectory);
        _fileSystem = new TestFileSystem(_tempRoot.FullName);
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero));
        _database = new AgentDatabase();
        _agentRegistry = new AgentRegistry(_fileSystem, _timeProvider, _database);
        _mail = new MailStore(_fileSystem, _timeProvider, _database, _agentRegistry);
        _ledger = new SessionDeliveryLedger(_fileSystem, _database);
        _leases = new PingLeaseStore(_fileSystem, _database);
        _peerClient = new FakeClaudePeerClient();
    }

    public void Dispose() => _tempRoot.Delete(recursive: true);

    [Fact]
    public async Task RunAsync_Should_ClaimSendReserveAndCleanUp_When_ALiveAncestorSessionExists()
    {
        // arrange: an ancestor session whose harness-derived peer name is a
        // valid endpoint address, so self-claim records it as claude-peer,
        // mirroring a real Claude Code session with peer messaging enabled.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var sessions = CreateSessionRegistry(new ClaudeAncestorSession(
            CurrentAlivePid(), "claude-session-1", _tempRoot.FullName, "peer-a"));
        var probe = CreateProbe(sessions);

        // act
        var result = await probe.RunAsync(cancellationToken);

        // assert: both delivery-ledger channels claimed the same message,
        // the probe as a whole succeeded, and the successful peer ping is
        // reported but never part of that success.
        Assert.True(result.DigestLedgerClaimed);
        Assert.True(result.GateLedgerClaimed);
        Assert.True(result.Success);
        Assert.Equal(AgentSessionEndpointKind.ClaudePeer, result.EndpointKind);
        Assert.Equal(AgentPingResult.Ok, result.PingResult);
        Assert.Equal(AgentSessionHarness.ClaudeCode, result.Harness);
        Assert.Equal("claude-session-1", result.SessionId);
        Assert.StartsWith("doctor-probe-", result.ScratchActor);
        Assert.Single(_peerClient.Calls);

        // the scratch claim is torn down: no row survives the probe.
        Assert.Equal(0, await CountSessionRowsAsync(cancellationToken));

        // the probe's own message is archived, not left as live unread mail.
        var message = await _mail.GetRequiredMessageAsync(result.MessageId, cancellationToken);
        Assert.All(message.Recipients, recipient => Assert.NotNull(recipient.ArchivedAt));
    }

    [Fact]
    public async Task RunAsync_Should_ReportSkippedNoEndpoint_When_AncestorNameIsNotAValidEndpointAddress()
    {
        // arrange: a name containing a space fails EndpointAddress.IsValid,
        // so self-claim records endpoint_kind 'none' - nothing to ping.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var sessions = CreateSessionRegistry(new ClaudeAncestorSession(
            CurrentAlivePid(), "claude-session-1", _tempRoot.FullName, "not a valid peer name"));
        var probe = CreateProbe(sessions);

        // act
        var result = await probe.RunAsync(cancellationToken);

        // assert
        Assert.Equal(AgentSessionEndpointKind.None, result.EndpointKind);
        Assert.Equal("skipped-no-endpoint", result.PingResult);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task RunAsync_Should_Throw_When_NoAncestorSessionIsFound()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var sessions = CreateSessionRegistry(ancestor: null);
        var probe = CreateProbe(sessions);

        // act & assert: the same actionable message SelfClaimAsync gives
        // `agent session claim`, never a raw stack trace.
        var exception = await Assert.ThrowsAsync<ExitException>(() => probe.RunAsync(cancellationToken));
        Assert.Contains("Could not identify a Claude Code ancestor session", exception.Message);
        Assert.Equal(0, await CountSessionRowsAsync(cancellationToken));
    }

    [Fact]
    public async Task RunAsync_Should_Throw_And_LeaveTheExistingClaimUntouched_When_AlreadyExplicitlyClaimedByAnotherActor()
    {
        // arrange: a real actor already explicitly owns this ancestor
        // session (e.g. a real hook already claimed it) before the probe
        // ever runs.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var ancestor = new ClaudeAncestorSession(CurrentAlivePid(), "claude-session-1", _tempRoot.FullName, "peer-a");
        var sessions = CreateSessionRegistry(ancestor);
        await sessions.SelfClaimAsync("real-orchestrator", forceRebind: false, cancellationToken);
        var probe = CreateProbe(sessions);

        // act & assert
        var exception = await Assert.ThrowsAsync<ExitException>(() => probe.RunAsync(cancellationToken));
        Assert.Contains("already explicitly claimed by", exception.Message);

        // the probe never got far enough to touch the row it did not
        // create: the real claim survives exactly as it was.
        var row = await FindRowAsync(cancellationToken);
        Assert.NotNull(row);
        Assert.Equal("real-orchestrator", row.AgentName);
    }

    private AgentSessionRegistry CreateSessionRegistry(ClaudeAncestorSession? ancestor) => new(
        _fileSystem,
        _timeProvider,
        _database,
        _agentRegistry,
        new FixedInstanceIdProvider(Host),
        new FixedGlobalConfigDirectoryProvider(_tempRoot.FullName),
        new ProcessInfoProvider(),
        new FixedAncestorSessionResolver(ancestor));

    private ClaudeRoundTripProbe CreateProbe(AgentSessionRegistry sessions)
    {
        var executor = new PingSessionExecutor(
            _mail,
            new FakeCodexQueueClient(),
            _peerClient,
            sessions,
            _leases,
            _timeProvider);

        return new ClaudeRoundTripProbe(
            _agentRegistry,
            sessions,
            _mail,
            _ledger,
            executor,
            _leases,
            _timeProvider);
    }

    private static int CurrentAlivePid() => Environment.ProcessId;

    private async Task InitializeWorkspaceAsync(CancellationToken cancellationToken)
    {
        await using (await _database.InitializeAsync(_workspaceDirectory, cancellationToken))
        {
        }
    }

    private async Task<long> CountSessionRowsAsync(CancellationToken cancellationToken)
    {
        await using var connection = await _database.ConnectAsync(_workspaceDirectory, cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM agent_sessions;";

        return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
    }

    private async Task<SessionRow?> FindRowAsync(CancellationToken cancellationToken)
    {
        await using var connection = await _database.ConnectAsync(_workspaceDirectory, cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT agent_name AS AgentName FROM agent_sessions LIMIT 1;";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new SessionRow(reader.IsDBNull(0) ? null : reader.GetString(0));
    }

    private sealed record SessionRow(string? AgentName);
}
