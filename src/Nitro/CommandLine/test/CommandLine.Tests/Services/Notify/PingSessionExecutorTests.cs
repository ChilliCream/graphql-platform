using ChilliCream.Nitro.CommandLine.Services.Hook;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Notify;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tests.Commands;
using ChilliCream.Nitro.CommandLine.Tests.Hook;
using Microsoft.Extensions.Time.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Exercises <see cref="PingSessionExecutor"/> end to end against a real
/// workspace database and mail store: digest construction from unread mail,
/// the codex-thread transport call, the hard-timeout path, result-write
/// conditioning, and lease release on every exit - the notifier's required
/// "a codex-thread ping wakes a live thread end to end" test.
/// </summary>
public sealed class PingSessionExecutorTests : IDisposable
{
    private const string Harness = AgentSessionHarness.Codex;
    private const string SessionId = "session-1";
    private const string Actor = "codex-worker";
    private const string ThreadId = "thread-1";

    private readonly DirectoryInfo _tempRoot;
    private readonly string _workspaceDirectory;
    private readonly TestFileSystem _fileSystem;
    private readonly FakeTimeProvider _timeProvider;
    private readonly AgentDatabase _database;
    private readonly AgentRegistry _agentRegistry;
    private readonly AgentSessionRegistry _sessions;
    private readonly MailStore _mail;
    private readonly PingLeaseStore _leases;
    private readonly FakeCodexQueueClient _queueClient;
    private readonly FakeClaudePeerClient _claudePeerClient;
    private readonly AgentSessionGeneration _generation;

    public PingSessionExecutorTests()
    {
        _tempRoot = Directory.CreateTempSubdirectory("nitro-ping-session-executor-tests");
        _workspaceDirectory = AgentWorkspace.GetDirectory(_tempRoot.FullName);
        Directory.CreateDirectory(_workspaceDirectory);
        _fileSystem = new TestFileSystem(_tempRoot.FullName);
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero));
        _database = new AgentDatabase();
        _agentRegistry = new AgentRegistry(_fileSystem, _timeProvider, _database);
        _sessions = new AgentSessionRegistry(
            _fileSystem,
            _timeProvider,
            _database,
            _agentRegistry,
            new FixedInstanceIdProvider("host-1"),
            new FixedGlobalConfigDirectoryProvider(_tempRoot.FullName));
        _mail = new MailStore(_fileSystem, _timeProvider, _database, _agentRegistry);
        _leases = new PingLeaseStore(_fileSystem, _database);
        _queueClient = new FakeCodexQueueClient();
        _claudePeerClient = new FakeClaudePeerClient();
        _generation = new AgentSessionGeneration(Harness, SessionId, "host-1");
    }

    public void Dispose() => _tempRoot.Delete(recursive: true);

    [Fact]
    public async Task ExecuteCodexThreadAsync_Should_QueueTheDigestAndRecordOk_When_UnreadMailExists()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeSessionAsync(cancellationToken);
        var message = await _mail.SendMessageAsync(
            new MailMessageCreation { Sender = "pascal", Subject = "status", Body = "check", To = [Actor] },
            cancellationToken);
        var attemptId = await ClaimAttemptAsync(cancellationToken);
        var slot = await _leases.TryAcquireAsync(
            attemptId, _timeProvider.GetUtcNow(), TimeSpan.FromSeconds(30), cancellationToken);
        var executor = CreateExecutor();

        // act
        var outcome = await executor.ExecuteCodexThreadAsync(
            Harness, SessionId, Actor, ThreadId, attemptId, slot!.Value, FarFutureDeadline(), cancellationToken);

        // assert
        Assert.Equal(AgentPingResult.Ok, outcome.Result);
        var call = Assert.Single(_queueClient.Calls);
        Assert.Equal(ThreadId, call.ThreadId);
        Assert.Contains("1 unread nitro message.", call.Message);
        Assert.Contains("nitro agent mail inbox --actor", call.Message);
        Assert.DoesNotContain(message.Id, call.Message);
        var row = await _sessions.FindByGenerationAsync(_generation, cancellationToken);
        Assert.Equal(AgentPingResult.Ok, row!.LastPingResult);
    }

    [Fact]
    public async Task ExecuteCodexThreadAsync_Should_ReleaseTheLease_When_ItCompletes()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeSessionAsync(cancellationToken);
        await _mail.SendMessageAsync(
            new MailMessageCreation { Sender = "pascal", Subject = "status", Body = "check", To = [Actor] },
            cancellationToken);
        var attemptId = await ClaimAttemptAsync(cancellationToken);
        var now = _timeProvider.GetUtcNow();
        var slot = await _leases.TryAcquireAsync(attemptId, now, TimeSpan.FromSeconds(30), cancellationToken);
        var executor = CreateExecutor();

        // act
        await executor.ExecuteCodexThreadAsync(
            Harness, SessionId, Actor, ThreadId, attemptId, slot!.Value, FarFutureDeadline(), cancellationToken);

        // assert: the slot is free again, so a fresh attempt reclaims the
        // exact same slot number.
        var reacquired = await _leases.TryAcquireAsync("attempt-next", now, TimeSpan.FromSeconds(30), cancellationToken);
        Assert.Equal(slot, reacquired);
    }

    [Fact]
    public async Task ExecuteCodexThreadAsync_Should_RecordError_When_TheTransportCallFails()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeSessionAsync(cancellationToken);
        await _mail.SendMessageAsync(
            new MailMessageCreation { Sender = "pascal", Subject = "status", Body = "check", To = [Actor] },
            cancellationToken);
        var attemptId = await ClaimAttemptAsync(cancellationToken);
        var slot = await _leases.TryAcquireAsync(
            attemptId, _timeProvider.GetUtcNow(), TimeSpan.FromSeconds(30), cancellationToken);
        _queueClient.NextResult = CodexQueueResult.Error;
        var executor = CreateExecutor();

        // act
        var outcome = await executor.ExecuteCodexThreadAsync(
            Harness, SessionId, Actor, ThreadId, attemptId, slot!.Value, FarFutureDeadline(), cancellationToken);

        // assert
        Assert.Equal(AgentPingResult.Error, outcome.Result);
    }

    [Fact]
    public async Task ExecuteCodexThreadAsync_Should_RecordEndpointGone_When_TheTransportSignalsGoneThread()
    {
        // arrange: fixture-evidenced signature for a dead/unknown codex
        // thread.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeSessionAsync(cancellationToken);
        await _mail.SendMessageAsync(
            new MailMessageCreation { Sender = "pascal", Subject = "status", Body = "check", To = [Actor] },
            cancellationToken);
        var attemptId = await ClaimAttemptAsync(cancellationToken);
        var slot = await _leases.TryAcquireAsync(
            attemptId, _timeProvider.GetUtcNow(), TimeSpan.FromSeconds(30), cancellationToken);
        _queueClient.NextResult = CodexQueueResult.EndpointGone;
        var executor = CreateExecutor();

        // act
        var outcome = await executor.ExecuteCodexThreadAsync(
            Harness, SessionId, Actor, ThreadId, attemptId, slot!.Value, FarFutureDeadline(), cancellationToken);

        // assert
        Assert.Equal(AgentPingResult.EndpointGone, outcome.Result);
        var row = await _sessions.FindByGenerationAsync(_generation, cancellationToken);
        Assert.Equal(AgentPingResult.EndpointGone, row!.LastPingResult);
    }

    [Fact]
    public async Task ExecuteClaudePeerAsync_Should_SendTheDigestAndRecordOk_When_UnreadMailExists()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeSessionAsync(cancellationToken);
        var message = await _mail.SendMessageAsync(
            new MailMessageCreation { Sender = "pascal", Subject = "status", Body = "check", To = [Actor] },
            cancellationToken);
        var attemptId = await ClaimAttemptAsync(cancellationToken);
        var slot = await _leases.TryAcquireAsync(
            attemptId, _timeProvider.GetUtcNow(), TimeSpan.FromSeconds(30), cancellationToken);
        var executor = CreateExecutor();

        // act
        var outcome = await executor.ExecuteClaudePeerAsync(
            Harness, SessionId, Actor, attemptId, slot!.Value, FarFutureDeadline(), cancellationToken);

        // assert
        Assert.Equal(AgentPingResult.Ok, outcome.Result);
        var call = Assert.Single(_claudePeerClient.Calls);
                Assert.Equal(SessionId, call.SessionId);
        Assert.Contains("1 unread nitro message.", call.Message);
        Assert.Contains("nitro agent mail inbox --actor", call.Message);
        Assert.DoesNotContain(message.Id, call.Message);
        var row = await _sessions.FindByGenerationAsync(_generation, cancellationToken);
        Assert.Equal(AgentPingResult.Ok, row!.LastPingResult);
    }

    [Fact]
    public async Task ExecuteClaudePeerAsync_Should_ReturnOutcomeMatchingTheDurableRow_When_ItSucceeds()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeSessionAsync(cancellationToken);
        await _mail.SendMessageAsync(
            new MailMessageCreation { Sender = "pascal", Subject = "status", Body = "check", To = [Actor] },
            cancellationToken);
        var attemptId = await ClaimAttemptAsync(cancellationToken);
        var slot = await _leases.TryAcquireAsync(
            attemptId, _timeProvider.GetUtcNow(), TimeSpan.FromSeconds(30), cancellationToken);
        var executor = CreateExecutor();

        // act
        var outcome = await executor.ExecuteClaudePeerAsync(
            Harness, SessionId, Actor, attemptId, slot!.Value, FarFutureDeadline(), cancellationToken);

        // assert: the typed outcome exactly matches what was durably written.
        var row = await _sessions.FindByGenerationAsync(_generation, cancellationToken);
        Assert.Equal(row!.LastPingResult, outcome.Result);
        Assert.Equal(row.LastPingDetail, outcome.Detail);
        Assert.Equal(Harness, outcome.Harness);
        Assert.Equal(SessionId, outcome.SessionId);
        Assert.Equal(attemptId, outcome.AttemptId);
    }

    [Fact]
    public async Task ExecuteClaudePeerAsync_Should_ReturnAccessDeniedReason_When_ThePeerClientReportsAccessDenied()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeSessionAsync(cancellationToken);
        await _mail.SendMessageAsync(
            new MailMessageCreation { Sender = "pascal", Subject = "status", Body = "check", To = [Actor] },
            cancellationToken);
        var attemptId = await ClaimAttemptAsync(cancellationToken);
        var slot = await _leases.TryAcquireAsync(
            attemptId, _timeProvider.GetUtcNow(), TimeSpan.FromSeconds(30), cancellationToken);
        _claudePeerClient.NextOutcome = ClaudePeerSendOutcome.AccessDenied;
        var executor = CreateExecutor();

        // act
        var outcome = await executor.ExecuteClaudePeerAsync(
            Harness, SessionId, Actor, attemptId, slot!.Value, FarFutureDeadline(), cancellationToken);

        // assert: the coarse CHECK-compatible result stays "error", but the
        // typed reason and detail stay specific.
        Assert.Equal(AgentPingResult.Error, outcome.Result);
        Assert.Equal(PingAttemptReason.AccessDenied, outcome.Reason);
        Assert.False(outcome.Retryable);
        Assert.Equal(ClaudePeerSendOutcome.AccessDenied.Detail, outcome.Detail);
        var row = await _sessions.FindByGenerationAsync(_generation, cancellationToken);
        Assert.Equal(row!.LastPingDetail, outcome.Detail);
    }

    [Fact]
    public async Task ExecuteClaudePeerAsync_Should_ReturnInvalidAuthReason_When_ThePeerClientReportsInvalidAuth()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeSessionAsync(cancellationToken);
        await _mail.SendMessageAsync(
            new MailMessageCreation { Sender = "pascal", Subject = "status", Body = "check", To = [Actor] },
            cancellationToken);
        var attemptId = await ClaimAttemptAsync(cancellationToken);
        var slot = await _leases.TryAcquireAsync(
            attemptId, _timeProvider.GetUtcNow(), TimeSpan.FromSeconds(30), cancellationToken);
        _claudePeerClient.NextOutcome = ClaudePeerSendOutcome.InvalidAuth;
        var executor = CreateExecutor();

        // act
        var outcome = await executor.ExecuteClaudePeerAsync(
            Harness, SessionId, Actor, attemptId, slot!.Value, FarFutureDeadline(), cancellationToken);

        // assert
        Assert.Equal(AgentPingResult.Error, outcome.Result);
        Assert.Equal(PingAttemptReason.InvalidAuth, outcome.Reason);
        Assert.False(outcome.Retryable);
    }

    [Fact]
    public async Task ExecuteCodexThreadAsync_Should_NotExposeTransportDetail_When_TheQueueClientReportsError()
    {
        // arrange: CodexQueueResult.Error can stem from the subprocess's own
        // stderr, but that raw text never reaches ICodexQueueClient's
        // caller, so the returned outcome must carry no detail either.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeSessionAsync(cancellationToken);
        await _mail.SendMessageAsync(
            new MailMessageCreation { Sender = "pascal", Subject = "status", Body = "check", To = [Actor] },
            cancellationToken);
        var attemptId = await ClaimAttemptAsync(cancellationToken);
        var slot = await _leases.TryAcquireAsync(
            attemptId, _timeProvider.GetUtcNow(), TimeSpan.FromSeconds(30), cancellationToken);
        _queueClient.NextResult = CodexQueueResult.Error;
        var executor = CreateExecutor();

        // act
        var outcome = await executor.ExecuteCodexThreadAsync(
            Harness, SessionId, Actor, ThreadId, attemptId, slot!.Value, FarFutureDeadline(), cancellationToken);

        // assert
        Assert.Equal(PingAttemptReason.TransportError, outcome.Reason);
        Assert.True(outcome.Retryable);
        Assert.Null(outcome.Detail);
    }

    [Fact]
    public async Task ExecuteClaudePeerAsync_Should_NotOverwriteTheRow_When_TheAttemptIsStale()
    {
        // arrange: a stale attempt (staleAttemptId) races a newer one
        // (newerAttemptId) that already reclaimed the row's cooldown by the
        // time the stale attempt's write lands.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeSessionAsync(cancellationToken);
        await _mail.SendMessageAsync(
            new MailMessageCreation { Sender = "pascal", Subject = "status", Body = "check", To = [Actor] },
            cancellationToken);
        var staleAttemptId = await ClaimAttemptAsync(cancellationToken);
        var slot = await _leases.TryAcquireAsync(
            staleAttemptId, _timeProvider.GetUtcNow(), TimeSpan.FromSeconds(30), cancellationToken);
        var freshSession = await _sessions.FindByGenerationAsync(_generation, cancellationToken);
        var newerAttemptId = $"attempt-{Guid.NewGuid():N}";
        await _sessions.TryClaimPingCooldownAsync(
            freshSession!,
            newerAttemptId,
            _timeProvider.GetUtcNow() + PingPolicy.Cooldown + TimeSpan.FromSeconds(1),
            PingPolicy.Cooldown,
            cancellationToken);
        var executor = CreateExecutor();

        // act: the stale attempt's own transport work still completes and
        // returns its own conclusion, even though the row has already moved
        // on to the newer attempt.
        var outcome = await executor.ExecuteClaudePeerAsync(
            Harness, SessionId, Actor, staleAttemptId, slot!.Value, FarFutureDeadline(), cancellationToken);

        // assert
        Assert.Equal(AgentPingResult.Ok, outcome.Result);
        var row = await _sessions.FindByGenerationAsync(_generation, cancellationToken);
        Assert.Equal(newerAttemptId, row!.LastPingAttempt);
        Assert.Null(row.LastPingResult);
    }

    [Fact]
    public async Task ExecuteCodexThreadAsync_Should_RecordOkWithoutCallingTheTransport_When_NoUnreadMailExists()
    {
        // arrange: the message that triggered this ping was already read by
        // the time the attempt ran - a benign race, not a failure.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeSessionAsync(cancellationToken);
        var attemptId = await ClaimAttemptAsync(cancellationToken);
        var slot = await _leases.TryAcquireAsync(
            attemptId, _timeProvider.GetUtcNow(), TimeSpan.FromSeconds(30), cancellationToken);
        var executor = CreateExecutor();

        // act
        var outcome = await executor.ExecuteCodexThreadAsync(
            Harness, SessionId, Actor, ThreadId, attemptId, slot!.Value, FarFutureDeadline(), cancellationToken);

        // assert
        Assert.Equal(AgentPingResult.Ok, outcome.Result);
        Assert.Empty(_queueClient.Calls);
    }

    [Fact]
    public async Task ExecuteCodexThreadAsync_Should_RecordTimeout_When_TheTransportCallOutlivesTheHardTimeout()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeSessionAsync(cancellationToken);
        await _mail.SendMessageAsync(
            new MailMessageCreation { Sender = "pascal", Subject = "status", Body = "check", To = [Actor] },
            cancellationToken);
        var attemptId = await ClaimAttemptAsync(cancellationToken);
        var slot = await _leases.TryAcquireAsync(
            attemptId, _timeProvider.GetUtcNow(), TimeSpan.FromSeconds(30), cancellationToken);
        var executor = new PingSessionExecutor(
            _mail,
            new NeverCompletingCodexQueueClient(),
            _claudePeerClient,
            _sessions,
            _leases,
            _timeProvider);

        // act
        var outcome = await executor.ExecuteCodexThreadAsync(
            Harness, SessionId, Actor, ThreadId, attemptId, slot!.Value,
            _timeProvider.GetUtcNow() + TimeSpan.FromMilliseconds(50), cancellationToken);

        // assert
        Assert.Equal(AgentPingResult.Timeout, outcome.Result);
        var row = await _sessions.FindByGenerationAsync(_generation, cancellationToken);
        Assert.Equal(AgentPingResult.Timeout, row!.LastPingResult);
    }

    [Fact]
    public async Task ExecuteCodexThreadAsync_Should_RecordTimeoutWithoutInvokingTheTransport_When_TheDeadlineIsAlreadyExpired()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeSessionAsync(cancellationToken);
        await _mail.SendMessageAsync(
            new MailMessageCreation { Sender = "pascal", Subject = "status", Body = "check", To = [Actor] },
            cancellationToken);
        var attemptId = await ClaimAttemptAsync(cancellationToken);
        var slot = await _leases.TryAcquireAsync(
            attemptId, _timeProvider.GetUtcNow(), TimeSpan.FromSeconds(30), cancellationToken);
        var executor = CreateExecutor();

        // act: startup latency across the process boundary already ate the
        // whole budget by the time this attempt runs.
        var outcome = await executor.ExecuteCodexThreadAsync(
            Harness, SessionId, Actor, ThreadId, attemptId, slot!.Value,
            _timeProvider.GetUtcNow() - TimeSpan.FromSeconds(1), cancellationToken);

        // assert
        Assert.Equal(AgentPingResult.Timeout, outcome.Result);
        Assert.Empty(_queueClient.Calls);
        var row = await _sessions.FindByGenerationAsync(_generation, cancellationToken);
        Assert.Equal(AgentPingResult.Timeout, row!.LastPingResult);
    }

    private PingSessionExecutor CreateExecutor()
        => new(_mail, _queueClient, _claudePeerClient, _sessions, _leases, _timeProvider);

    /// <summary>
    /// A deadline generous enough that a test's own real-time transport work
    /// never approaches it, so the digest/transport path runs to completion
    /// instead of racing the timeout.
    /// </summary>
    private DateTimeOffset FarFutureDeadline() => _timeProvider.GetUtcNow() + TimeSpan.FromSeconds(5);

    private async Task InitializeSessionAsync(CancellationToken cancellationToken)
    {
        await using (await _database.InitializeAsync(_workspaceDirectory, cancellationToken))
        {
        }

        await _sessions.StartAsync(
            _generation, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.CodexThread, ThreadId,
            envActor: Actor, cancellationToken);
    }

    /// <summary>
    /// Claims the cooldown to obtain a fresh attempt id conditioning the
    /// eventual result write, mirroring what the notifier does before
    /// spawning (or, here, directly invoking) the executor.
    /// </summary>
    private async Task<string> ClaimAttemptAsync(CancellationToken cancellationToken)
    {
        var session = await _sessions.FindByGenerationAsync(_generation, cancellationToken);
        var attemptId = $"attempt-{Guid.NewGuid():N}";
        await _sessions.TryClaimPingCooldownAsync(
            session!, attemptId, _timeProvider.GetUtcNow(), TimeSpan.FromSeconds(60), cancellationToken);
        return attemptId;
    }
}

/// <summary>
/// Never returns, so a caller racing it against its own timeout always
/// observes the timeout side of that race.
/// </summary>
internal sealed class NeverCompletingCodexQueueClient : ICodexQueueClient
{
    public async Task<CodexQueueResult> QueueAsync(string threadId, string message, CancellationToken cancellationToken)
    {
        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        return CodexQueueResult.Ok;
    }
}

internal sealed record FakeClaudePeerCall(string SessionId, string Message);

internal sealed class FakeClaudePeerClient : IClaudePeerClient
{
    public List<FakeClaudePeerCall> Calls { get; } = [];

    public ClaudePeerSendOutcome NextOutcome { get; set; } = ClaudePeerSendOutcome.Ok;

    public Task<ClaudePeerSendOutcome> SendAsync(
        string sessionId,
        string message,
        CancellationToken cancellationToken)
    {
        Calls.Add(new FakeClaudePeerCall(sessionId, message));
        return Task.FromResult(NextOutcome);
    }
}
