using ChilliCream.Nitro.CommandLine.Services.Hook;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Notify;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tests.Hook;
using Microsoft.Extensions.Time.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Tests <see cref="PingSessionExecutor"/> with a real workspace database
/// and mail store, including digest construction, transport outcomes,
/// conditional result writes, and lease release.
/// </summary>
public sealed class PingSessionExecutorTests : IDisposable
{
    private const string ThreadId = "thread-1";
    private const string OpencodeSessionId = "opencode-session-1";
    private const string OpencodeServerUrl = "http://127.0.0.1:4096";

    private readonly DirectoryInfo _tempRoot;
    private readonly string _workspaceDirectory;
    private readonly TestFileSystem _fileSystem;
    private readonly FakeTimeProvider _timeProvider;
    private readonly AgentDatabase _database;
    private readonly AgentStore _agentStore;
    private readonly MailStore _mail;
    private readonly AgentDeliveryLedger _ledger;
    private readonly PingLeaseStore _leases;
    private readonly FakeCodexQueueClient _queueClient;
    private readonly FakeClaudePeerClient _claudePeerClient;
    private readonly FakeOpencodeServerClient _opencodeClient;

    public PingSessionExecutorTests()
    {
        _tempRoot = Directory.CreateTempSubdirectory("nitro-ping-session-executor-tests");
        _workspaceDirectory = AgentWorkspace.GetDirectory(_tempRoot.FullName);
        Directory.CreateDirectory(_workspaceDirectory);
        _fileSystem = new TestFileSystem(_tempRoot.FullName);
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero));
        _database = new AgentDatabase();
        _agentStore = new AgentStore(_fileSystem, _timeProvider, _database);
        _mail = new MailStore(_fileSystem, _timeProvider, _database, _agentStore);
        _ledger = new AgentDeliveryLedger(_fileSystem, _database);
        _leases = new PingLeaseStore(_fileSystem, _database);
        _queueClient = new FakeCodexQueueClient();
        _claudePeerClient = new FakeClaudePeerClient();
        _opencodeClient = new FakeOpencodeServerClient();
    }

    public void Dispose() => _tempRoot.Delete(recursive: true);

    [Fact]
    public async Task ExecuteCodexThreadAsync_Should_QueueTheDigestAndRecordOk_When_UnreadMailExists()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var actor = await InitializeAgentAsync(cancellationToken);
        var message = await _mail.SendMessageAsync(
            new MailMessageCreation { Sender = "pascal", Subject = "status", Body = "check", To = [actor] },
            cancellationToken);
        var attemptId = await ClaimAttemptAsync(actor, cancellationToken);
        var slot = await _leases.TryAcquireAsync(
            attemptId, _timeProvider.GetUtcNow(), TimeSpan.FromSeconds(30), cancellationToken);
        var executor = CreateExecutor();

        // act
        var outcome = await executor.ExecuteCodexThreadAsync(
            actor, ThreadId, attemptId, slot!.Value, FarFutureDeadline(), cancellationToken);

        // assert
        var call = Assert.Single(_queueClient.Calls);
        Assert.Equal((ThreadId, message.Id, "check"), Digest(call.ThreadId, call.Message));
        var row = await _agentStore.FindAsync(actor, cancellationToken);
        Assert.Equal((AgentPingResult.Ok, AgentPingResult.Ok), (outcome.Result, row!.LastPingResult));
    }

    [Fact]
    public async Task ExecuteCodexThreadAsync_Should_ReleaseTheLease_When_ItCompletes()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var actor = await InitializeAgentAsync(cancellationToken);
        await _mail.SendMessageAsync(
            new MailMessageCreation { Sender = "pascal", Subject = "status", Body = "check", To = [actor] },
            cancellationToken);
        var attemptId = await ClaimAttemptAsync(actor, cancellationToken);
        var now = _timeProvider.GetUtcNow();
        var slot = await _leases.TryAcquireAsync(attemptId, now, TimeSpan.FromSeconds(30), cancellationToken);
        var executor = CreateExecutor();

        // act
        await executor.ExecuteCodexThreadAsync(
            actor, ThreadId, attemptId, slot!.Value, FarFutureDeadline(), cancellationToken);

        // assert
        // the slot is free again, so a fresh attempt reclaims the
        // exact same slot number.
        var reacquired = await _leases.TryAcquireAsync("attempt-next", now, TimeSpan.FromSeconds(30), cancellationToken);
        Assert.Equal(slot, reacquired);
    }

    [Fact]
    public async Task ExecuteCodexThreadAsync_Should_RecordError_When_TheTransportCallFails()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var actor = await InitializeAgentAsync(cancellationToken);
        await _mail.SendMessageAsync(
            new MailMessageCreation { Sender = "pascal", Subject = "status", Body = "check", To = [actor] },
            cancellationToken);
        var attemptId = await ClaimAttemptAsync(actor, cancellationToken);
        var slot = await _leases.TryAcquireAsync(
            attemptId, _timeProvider.GetUtcNow(), TimeSpan.FromSeconds(30), cancellationToken);
        _queueClient.NextResult = CodexQueueResult.Error;
        var executor = CreateExecutor();

        // act
        var outcome = await executor.ExecuteCodexThreadAsync(
            actor, ThreadId, attemptId, slot!.Value, FarFutureDeadline(), cancellationToken);

        // assert
        Assert.Equal(AgentPingResult.Error, outcome.Result);
    }

    [Fact]
    public async Task ExecuteCodexThreadAsync_Should_RecordEndpointGone_When_TheTransportSignalsGoneThread()
    {
        // arrange
        // Fixture-evidenced signature for a dead/unknown codex thread.
        var cancellationToken = TestContext.Current.CancellationToken;
        var actor = await InitializeAgentAsync(cancellationToken);
        await _mail.SendMessageAsync(
            new MailMessageCreation { Sender = "pascal", Subject = "status", Body = "check", To = [actor] },
            cancellationToken);
        var attemptId = await ClaimAttemptAsync(actor, cancellationToken);
        var slot = await _leases.TryAcquireAsync(
            attemptId, _timeProvider.GetUtcNow(), TimeSpan.FromSeconds(30), cancellationToken);
        _queueClient.NextResult = CodexQueueResult.EndpointGone;
        var executor = CreateExecutor();

        // act
        var outcome = await executor.ExecuteCodexThreadAsync(
            actor, ThreadId, attemptId, slot!.Value, FarFutureDeadline(), cancellationToken);

        // assert
        Assert.Equal(AgentPingResult.EndpointGone, outcome.Result);
        var row = await _agentStore.FindAsync(actor, cancellationToken);
        Assert.Equal(AgentPingResult.EndpointGone, row!.LastPingResult);
    }

    [Fact]
    public async Task ExecuteClaudePeerAsync_Should_SendTheDigestAndRecordOk_When_UnreadMailExists()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var actor = await InitializeAgentAsync(cancellationToken);
        var message = await _mail.SendMessageAsync(
            new MailMessageCreation { Sender = "pascal", Subject = "status", Body = "check", To = [actor] },
            cancellationToken);
        var attemptId = await ClaimAttemptAsync(actor, cancellationToken);
        var slot = await _leases.TryAcquireAsync(
            attemptId, _timeProvider.GetUtcNow(), TimeSpan.FromSeconds(30), cancellationToken);
        var executor = CreateExecutor();

        // act
        var outcome = await executor.ExecuteClaudePeerAsync(
            actor, "peer-session", attemptId, slot!.Value, FarFutureDeadline(), cancellationToken);

        // assert
        var call = Assert.Single(_claudePeerClient.Calls);
        Assert.Equal(("peer-session", message.Id, "check"), Digest(call.SessionId, call.Message));
        var row = await _agentStore.FindAsync(actor, cancellationToken);
        Assert.Equal((AgentPingResult.Ok, AgentPingResult.Ok), (outcome.Result, row!.LastPingResult));
    }

    [Fact]
    public async Task ExecuteClaudePeerAsync_Should_ReturnOutcomeMatchingTheDurableRow_When_ItSucceeds()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var actor = await InitializeAgentAsync(cancellationToken);
        await _mail.SendMessageAsync(
            new MailMessageCreation { Sender = "pascal", Subject = "status", Body = "check", To = [actor] },
            cancellationToken);
        var attemptId = await ClaimAttemptAsync(actor, cancellationToken);
        var slot = await _leases.TryAcquireAsync(
            attemptId, _timeProvider.GetUtcNow(), TimeSpan.FromSeconds(30), cancellationToken);
        var executor = CreateExecutor();

        // act
        var outcome = await executor.ExecuteClaudePeerAsync(
            actor, "peer-session", attemptId, slot!.Value, FarFutureDeadline(), cancellationToken);

        // assert
        // the typed outcome exactly matches what was durably written.
        var row = await _agentStore.FindAsync(actor, cancellationToken);
        Assert.Equal(row!.LastPingResult, outcome.Result);
        Assert.Equal(row.LastPingDetail, outcome.Detail);
        Assert.Equal(actor, outcome.ActorName);
        Assert.Equal(attemptId, outcome.AttemptId);
    }

    [Fact]
    public async Task ExecuteClaudePeerAsync_Should_ReturnAccessDeniedReason_When_ThePeerClientReportsAccessDenied()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var actor = await InitializeAgentAsync(cancellationToken);
        await _mail.SendMessageAsync(
            new MailMessageCreation { Sender = "pascal", Subject = "status", Body = "check", To = [actor] },
            cancellationToken);
        var attemptId = await ClaimAttemptAsync(actor, cancellationToken);
        var slot = await _leases.TryAcquireAsync(
            attemptId, _timeProvider.GetUtcNow(), TimeSpan.FromSeconds(30), cancellationToken);
        _claudePeerClient.NextOutcome = ClaudePeerSendOutcome.AccessDenied;
        var executor = CreateExecutor();

        // act
        var outcome = await executor.ExecuteClaudePeerAsync(
            actor, "peer-session", attemptId, slot!.Value, FarFutureDeadline(), cancellationToken);

        // assert
        // the coarse CHECK-compatible result stays "error", but the
        // typed reason and detail stay specific.
        Assert.Equal(AgentPingResult.Error, outcome.Result);
        Assert.Equal(PingAttemptReason.AccessDenied, outcome.Reason);
        Assert.False(outcome.Retryable);
        Assert.Equal(ClaudePeerSendOutcome.AccessDenied.Detail, outcome.Detail);
        var row = await _agentStore.FindAsync(actor, cancellationToken);
        Assert.Equal(row!.LastPingDetail, outcome.Detail);
    }

    [Fact]
    public async Task ExecuteClaudePeerAsync_Should_ReturnInvalidAuthReason_When_ThePeerClientReportsInvalidAuth()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var actor = await InitializeAgentAsync(cancellationToken);
        await _mail.SendMessageAsync(
            new MailMessageCreation { Sender = "pascal", Subject = "status", Body = "check", To = [actor] },
            cancellationToken);
        var attemptId = await ClaimAttemptAsync(actor, cancellationToken);
        var slot = await _leases.TryAcquireAsync(
            attemptId, _timeProvider.GetUtcNow(), TimeSpan.FromSeconds(30), cancellationToken);
        _claudePeerClient.NextOutcome = ClaudePeerSendOutcome.InvalidAuth;
        var executor = CreateExecutor();

        // act
        var outcome = await executor.ExecuteClaudePeerAsync(
            actor, "peer-session", attemptId, slot!.Value, FarFutureDeadline(), cancellationToken);

        // assert
        Assert.Equal(AgentPingResult.Error, outcome.Result);
        Assert.Equal(PingAttemptReason.InvalidAuth, outcome.Reason);
        Assert.False(outcome.Retryable);
    }

    [Fact]
    public async Task ExecuteCodexThreadAsync_Should_NotExposeTransportDetail_When_TheQueueClientReportsError()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var actor = await InitializeAgentAsync(cancellationToken);
        await _mail.SendMessageAsync(
            new MailMessageCreation { Sender = "pascal", Subject = "status", Body = "check", To = [actor] },
            cancellationToken);
        var attemptId = await ClaimAttemptAsync(actor, cancellationToken);
        var slot = await _leases.TryAcquireAsync(
            attemptId, _timeProvider.GetUtcNow(), TimeSpan.FromSeconds(30), cancellationToken);
        _queueClient.NextResult = CodexQueueResult.Error;
        var executor = CreateExecutor();

        // act
        var outcome = await executor.ExecuteCodexThreadAsync(
            actor, ThreadId, attemptId, slot!.Value, FarFutureDeadline(), cancellationToken);

        // assert
        Assert.Equal(PingAttemptReason.TransportError, outcome.Reason);
        Assert.True(outcome.Retryable);
        Assert.Null(outcome.Detail);
    }

    [Fact]
    public async Task ExecuteClaudePeerAsync_Should_NotOverwriteTheRow_When_TheAttemptIsStale()
    {
        // arrange
        // A newer attempt claims the cooldown before the stale attempt executes.
        var cancellationToken = TestContext.Current.CancellationToken;
        var actor = await InitializeAgentAsync(cancellationToken);
        await _mail.SendMessageAsync(
            new MailMessageCreation { Sender = "pascal", Subject = "status", Body = "check", To = [actor] },
            cancellationToken);
        var staleAttemptId = await ClaimAttemptAsync(actor, cancellationToken);
        var slot = await _leases.TryAcquireAsync(
            staleAttemptId, _timeProvider.GetUtcNow(), TimeSpan.FromSeconds(30), cancellationToken);
        // The claim's own cooldown gate requires the prior attempt to have aged past
        // it, so the clock advances before the newer attempt claims the cooldown.
        _timeProvider.Advance(PingPolicy.Cooldown + TimeSpan.FromSeconds(1));
        var newerAttemptId = $"attempt-{Guid.NewGuid():N}";
        await _agentStore.TryClaimPingCooldownAsync(
            actor, PingPolicy.Cooldown, newerAttemptId, cancellationToken);
        var executor = CreateExecutor();

        // act
        var outcome = await executor.ExecuteClaudePeerAsync(
            actor, "peer-session", staleAttemptId, slot!.Value, FarFutureDeadline(), cancellationToken);

        // assert
        Assert.Equal(AgentPingResult.Ok, outcome.Result);
        var row = await _agentStore.FindAsync(actor, cancellationToken);
        Assert.Equal(newerAttemptId, row!.LastPingAttempt);
        Assert.Null(row.LastPingResult);
    }

    [Fact]
    public async Task ExecuteCodexThreadAsync_Should_RecordOkWithoutCallingTheTransport_When_NoUnreadMailExists()
    {
        // arrange
        // The inbox is empty.
        var cancellationToken = TestContext.Current.CancellationToken;
        var actor = await InitializeAgentAsync(cancellationToken);
        var attemptId = await ClaimAttemptAsync(actor, cancellationToken);
        var slot = await _leases.TryAcquireAsync(
            attemptId, _timeProvider.GetUtcNow(), TimeSpan.FromSeconds(30), cancellationToken);
        var executor = CreateExecutor();

        // act
        var outcome = await executor.ExecuteCodexThreadAsync(
            actor, ThreadId, attemptId, slot!.Value, FarFutureDeadline(), cancellationToken);

        // assert
        Assert.Equal(AgentPingResult.Ok, outcome.Result);
        Assert.Empty(_queueClient.Calls);
    }

    [Fact]
    public async Task ExecuteCodexThreadAsync_Should_RecordTimeout_When_TheTransportCallOutlivesTheHardTimeout()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var actor = await InitializeAgentAsync(cancellationToken);
        await _mail.SendMessageAsync(
            new MailMessageCreation { Sender = "pascal", Subject = "status", Body = "check", To = [actor] },
            cancellationToken);
        var attemptId = await ClaimAttemptAsync(actor, cancellationToken);
        var slot = await _leases.TryAcquireAsync(
            attemptId, _timeProvider.GetUtcNow(), TimeSpan.FromSeconds(30), cancellationToken);
        var executor = new PingSessionExecutor(
            _mail,
            _ledger,
            new NeverCompletingCodexQueueClient(),
            _claudePeerClient,
            _agentStore,
            _leases,
            _timeProvider,
            _opencodeClient);

        // act
        var outcome = await executor.ExecuteCodexThreadAsync(
            actor, ThreadId, attemptId, slot!.Value,
            _timeProvider.GetUtcNow() + TimeSpan.FromMilliseconds(50), cancellationToken);

        // assert
        Assert.Equal(AgentPingResult.Timeout, outcome.Result);
        var row = await _agentStore.FindAsync(actor, cancellationToken);
        Assert.Equal(AgentPingResult.Timeout, row!.LastPingResult);
    }

    [Fact]
    public async Task ExecuteCodexThreadAsync_Should_RecordTimeoutWithoutInvokingTheTransport_When_TheDeadlineIsAlreadyExpired()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var actor = await InitializeAgentAsync(cancellationToken);
        await _mail.SendMessageAsync(
            new MailMessageCreation { Sender = "pascal", Subject = "status", Body = "check", To = [actor] },
            cancellationToken);
        var attemptId = await ClaimAttemptAsync(actor, cancellationToken);
        var slot = await _leases.TryAcquireAsync(
            attemptId, _timeProvider.GetUtcNow(), TimeSpan.FromSeconds(30), cancellationToken);
        var executor = CreateExecutor();

        // act
        // startup latency across the process boundary already ate the
        // whole budget by the time this attempt runs.
        var outcome = await executor.ExecuteCodexThreadAsync(
            actor, ThreadId, attemptId, slot!.Value,
            _timeProvider.GetUtcNow() - TimeSpan.FromSeconds(1), cancellationToken);

        // assert
        Assert.Equal(AgentPingResult.Timeout, outcome.Result);
        Assert.Empty(_queueClient.Calls);
        var row = await _agentStore.FindAsync(actor, cancellationToken);
        Assert.Equal(AgentPingResult.Timeout, row!.LastPingResult);
    }

    [Fact]
    public async Task ExecuteCodexThreadAsync_Should_LeaveTheMessageUnread_When_ItQueuesTheDigest()
    {
        // arrange
        // pushing the body to the thread never means it was read.
        var cancellationToken = TestContext.Current.CancellationToken;
        var actor = await InitializeAgentAsync(cancellationToken);
        var message = await _mail.SendMessageAsync(
            new MailMessageCreation { Sender = "pascal", Subject = "status", Body = "check", To = [actor] },
            cancellationToken);
        var unreadBefore = await _mail.CountUnreadAsync(actor, cancellationToken);
        var attemptId = await ClaimAttemptAsync(actor, cancellationToken);
        var slot = await _leases.TryAcquireAsync(
            attemptId, _timeProvider.GetUtcNow(), TimeSpan.FromSeconds(30), cancellationToken);
        var executor = CreateExecutor();

        // act
        await executor.ExecuteCodexThreadAsync(
            actor, ThreadId, attemptId, slot!.Value, FarFutureDeadline(), cancellationToken);

        // assert
        // the pushed payload says unread, the message is still in the
        // unread inbox, and the unread count is unchanged.
        var call = Assert.Single(_queueClient.Calls);
        Assert.False(DigestRead(call.Message));
        var unread = await _mail.QueryInboxAsync(
            new MailInboxFilter { Actor = actor, UnreadOnly = true }, cancellationToken);
        Assert.Contains(unread, m => m.Id == message.Id);
        Assert.Equal(unreadBefore, await _mail.CountUnreadAsync(actor, cancellationToken));
    }

    private PingSessionExecutor CreateExecutor()
        => new(
            _mail, _ledger, _queueClient, _claudePeerClient, _agentStore, _leases, _timeProvider, _opencodeClient);

    private static (string Endpoint, string Id, string Body) Digest(string endpoint, string digest)
    {
        using var document = System.Text.Json.JsonDocument.Parse(digest[(digest.IndexOf('\n') + 1)..]);
        var item = document.RootElement.GetProperty("items")[0];

        return (endpoint, item.GetProperty("id").GetString()!, item.GetProperty("body").GetString()!);
    }

    private static bool DigestRead(string digest)
    {
        using var document = System.Text.Json.JsonDocument.Parse(digest[(digest.IndexOf('\n') + 1)..]);

        return document.RootElement.GetProperty("items")[0].GetProperty("read").GetBoolean();
    }

    /// <summary>
    /// A deadline generous enough that a test's own real-time transport work
    /// never approaches it, so the digest/transport path runs to completion
    /// instead of racing the timeout.
    /// </summary>
    private DateTimeOffset FarFutureDeadline() => _timeProvider.GetUtcNow() + TimeSpan.FromSeconds(5);

    /// <summary>
    /// Initializes the workspace and mints an agent row with a Codex
    /// harness session, returning its allocated name.
    /// </summary>
    private async Task<string> InitializeAgentAsync(CancellationToken cancellationToken)
    {
        await using (await _database.InitializeAsync(_workspaceDirectory, cancellationToken))
        {
        }

        var result = await _agentStore.StartSessionAsync(
            new AgentSessionStartRequest
            {
                Harness = AgentSessionHarness.Codex,
                SessionId = "session-1",
                HarnessVersion = "1.0.0",
                Cwd = "/work",
                WorkspacePath = "/work/.nitro/agents",
                EndpointKind = AgentSessionEndpointKind.CodexThread,
                EndpointAddr = ThreadId
            },
            cancellationToken);

        return result.Row!.Name;
    }

    /// <summary>
    /// Initializes the workspace and mints an agent row with an opencode
    /// harness session, returning its allocated name.
    /// </summary>
    private async Task<string> InitializeOpencodeAgentAsync(string? secret, CancellationToken cancellationToken)
    {
        await using (await _database.InitializeAsync(_workspaceDirectory, cancellationToken))
        {
        }

        var result = await _agentStore.StartSessionAsync(
            new AgentSessionStartRequest
            {
                Harness = AgentSessionHarness.Opencode,
                SessionId = OpencodeSessionId,
                HarnessVersion = "1.0.0",
                Cwd = "/work",
                WorkspacePath = "/work/.nitro/agents",
                EndpointKind = AgentSessionEndpointKind.OpencodeServer,
                EndpointAddr = OpencodeServerUrl,
                EndpointSecret = secret
            },
            cancellationToken);

        return result.Row!.Name;
    }

    /// <summary>
    /// Claims the cooldown to obtain a fresh attempt id conditioning the
    /// eventual result write, mirroring what the notifier does before
    /// spawning (or, here, directly invoking) the executor.
    /// </summary>
    private async Task<string> ClaimAttemptAsync(string actor, CancellationToken cancellationToken)
    {
        var attemptId = $"attempt-{Guid.NewGuid():N}";
        await _agentStore.TryClaimPingCooldownAsync(actor, TimeSpan.FromSeconds(60), attemptId, cancellationToken);
        return attemptId;
    }

    [Fact]
    public async Task ExecuteOpencodeServerAsync_Should_PushThePrefixedDigestAndRecordOk_When_UnreadMailExists()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var actor = await InitializeOpencodeAgentAsync("s3cret", cancellationToken);
        await _mail.SendMessageAsync(
            new MailMessageCreation { Sender = "pascal", Subject = "status", Body = "check", To = [actor] },
            cancellationToken);
        var attemptId = await ClaimAttemptAsync(actor, cancellationToken);
        var slot = await _leases.TryAcquireAsync(
            attemptId, _timeProvider.GetUtcNow(), TimeSpan.FromSeconds(30), cancellationToken);
        var executor = CreateExecutor();

        // act
        var outcome = await executor.ExecuteOpencodeServerAsync(
            actor, OpencodeSessionId, OpencodeServerUrl, "s3cret",
            attemptId, slot!.Value, FarFutureDeadline(), cancellationToken);

        // assert
        Assert.Equal(AgentPingResult.Ok, outcome.Result);
        var call = Assert.Single(_opencodeClient.PushCalls);
        Assert.Equal((OpencodeServerUrl, OpencodeSessionId, "s3cret"), (call.ServerUrl, call.SessionId, call.Secret));
        Assert.StartsWith(OpencodeHookProtocol.PushedPromptPrefix, call.Text, StringComparison.Ordinal);
        Assert.Contains("1 unread nitro message;", call.Text);
        Assert.Contains("check", call.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteOpencodeServerAsync_Should_PingWithoutPushing_When_NoUnreadMailExists()
    {
        // arrange
        // the mail that triggered this attempt was already read by
        // the time it ran, so this is a health-only ping, not a delivery.
        var cancellationToken = TestContext.Current.CancellationToken;
        var actor = await InitializeOpencodeAgentAsync(secret: null, cancellationToken);
        var attemptId = await ClaimAttemptAsync(actor, cancellationToken);
        var slot = await _leases.TryAcquireAsync(
            attemptId, _timeProvider.GetUtcNow(), TimeSpan.FromSeconds(30), cancellationToken);
        var executor = CreateExecutor();

        // act
        var outcome = await executor.ExecuteOpencodeServerAsync(
            actor, OpencodeSessionId, OpencodeServerUrl, null,
            attemptId, slot!.Value, FarFutureDeadline(), cancellationToken);

        // assert
        Assert.Equal(AgentPingResult.Ok, outcome.Result);
        var call = Assert.Single(_opencodeClient.PingCalls);
        Assert.Equal(OpencodeServerUrl, call.ServerUrl);
        Assert.Equal(OpencodeSessionId, call.SessionId);
        Assert.Empty(_opencodeClient.PushCalls);
    }

    [Fact]
    public async Task ExecuteOpencodeServerAsync_Should_RecordEndpointGone_When_ThePingSignalsAMissingOrStaleEndpoint()
    {
        // arrange
        // The inbox is empty and the fake client reports EndpointGone.
        var cancellationToken = TestContext.Current.CancellationToken;
        var actor = await InitializeOpencodeAgentAsync(secret: null, cancellationToken);
        var attemptId = await ClaimAttemptAsync(actor, cancellationToken);
        var slot = await _leases.TryAcquireAsync(
            attemptId, _timeProvider.GetUtcNow(), TimeSpan.FromSeconds(30), cancellationToken);
        _opencodeClient.NextPingResult = AgentPingResult.EndpointGone;
        var executor = CreateExecutor();

        // act
        var outcome = await executor.ExecuteOpencodeServerAsync(
            actor, OpencodeSessionId, OpencodeServerUrl, null,
            attemptId, slot!.Value, FarFutureDeadline(), cancellationToken);

        // assert
        Assert.Equal(AgentPingResult.EndpointGone, outcome.Result);
        var row = await _agentStore.FindAsync(actor, cancellationToken);
        Assert.Equal(AgentPingResult.EndpointGone, row!.LastPingResult);
    }

    [Fact]
    public async Task ExecuteOpencodeServerAsync_Should_RecordEndpointGone_When_ThePushSignalsAMissingOrStaleEndpoint()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var actor = await InitializeOpencodeAgentAsync(secret: null, cancellationToken);
        await _mail.SendMessageAsync(
            new MailMessageCreation { Sender = "pascal", Subject = "status", Body = "check", To = [actor] },
            cancellationToken);
        var attemptId = await ClaimAttemptAsync(actor, cancellationToken);
        var slot = await _leases.TryAcquireAsync(
            attemptId, _timeProvider.GetUtcNow(), TimeSpan.FromSeconds(30), cancellationToken);
        _opencodeClient.NextPushResult = AgentPingResult.EndpointGone;
        var executor = CreateExecutor();

        // act
        var outcome = await executor.ExecuteOpencodeServerAsync(
            actor, OpencodeSessionId, OpencodeServerUrl, null,
            attemptId, slot!.Value, FarFutureDeadline(), cancellationToken);

        // assert
        Assert.Equal(AgentPingResult.EndpointGone, outcome.Result);
        Assert.Equal(PingAttemptReason.EndpointGone, outcome.Reason);
    }

    [Fact]
    public async Task ExecuteOpencodeServerAsync_Should_RecordTimeout_When_ThePushSignalsATimeout()
    {
        // arrange
        // proves the ok/timeout/error result vocabulary maps
        // through unchanged for the opencode transport too.
        var cancellationToken = TestContext.Current.CancellationToken;
        var actor = await InitializeOpencodeAgentAsync(secret: null, cancellationToken);
        await _mail.SendMessageAsync(
            new MailMessageCreation { Sender = "pascal", Subject = "status", Body = "check", To = [actor] },
            cancellationToken);
        var attemptId = await ClaimAttemptAsync(actor, cancellationToken);
        var slot = await _leases.TryAcquireAsync(
            attemptId, _timeProvider.GetUtcNow(), TimeSpan.FromSeconds(30), cancellationToken);
        _opencodeClient.NextPushResult = AgentPingResult.Timeout;
        var executor = CreateExecutor();

        // act
        var outcome = await executor.ExecuteOpencodeServerAsync(
            actor, OpencodeSessionId, OpencodeServerUrl, null,
            attemptId, slot!.Value, FarFutureDeadline(), cancellationToken);

        // assert
        Assert.Equal(AgentPingResult.Timeout, outcome.Result);
        Assert.True(outcome.Retryable);
    }

    [Fact]
    public async Task ExecuteOpencodeServerAsync_Should_RewriteTheRow_When_TheHealthOnlyPingRepeatsThePriorOutcome()
    {
        // arrange
        // an earlier health-only ping already recorded ok/health-only.
        var cancellationToken = TestContext.Current.CancellationToken;
        var actor = await InitializeOpencodeAgentAsync(secret: null, cancellationToken);
        var firstAttemptId = await ClaimAttemptAsync(actor, cancellationToken);
        var firstSlot = await _leases.TryAcquireAsync(
            firstAttemptId, _timeProvider.GetUtcNow(), TimeSpan.FromSeconds(30), cancellationToken);
        var executor = CreateExecutor();
        await executor.ExecuteOpencodeServerAsync(
            actor, OpencodeSessionId, OpencodeServerUrl, null,
            firstAttemptId, firstSlot!.Value, FarFutureDeadline(), cancellationToken);

        // act
        // A fresh claim, then a health-only ping that reports the same outcome as before.
        _timeProvider.Advance(TimeSpan.FromSeconds(61));
        var secondAttemptId = await ClaimAttemptAsync(actor, cancellationToken);
        var secondSlot = await _leases.TryAcquireAsync(
            secondAttemptId, _timeProvider.GetUtcNow(), TimeSpan.FromSeconds(30), cancellationToken);
        var outcome = await executor.ExecuteOpencodeServerAsync(
            actor, OpencodeSessionId, OpencodeServerUrl, null,
            secondAttemptId, secondSlot!.Value, FarFutureDeadline(), cancellationToken);

        // assert
        Assert.Equal(AgentPingResult.Ok, outcome.Result);
        var row = await _agentStore.FindAsync(actor, cancellationToken);
        Assert.Equal(AgentPingResult.Ok, row!.LastPingResult);
        Assert.Equal(PingSessionExecutor.HealthOnlyDetail, row.LastPingDetail);
    }

    [Fact]
    public async Task ExecuteOpencodeServerAsync_Should_WriteTheRow_When_TheHealthOnlyPingFirstReachesItsOkState()
    {
        // arrange
        // no known previous state (a fresh row's first ping ever).
        var cancellationToken = TestContext.Current.CancellationToken;
        var actor = await InitializeOpencodeAgentAsync(secret: null, cancellationToken);
        var attemptId = await ClaimAttemptAsync(actor, cancellationToken);
        var slot = await _leases.TryAcquireAsync(
            attemptId, _timeProvider.GetUtcNow(), TimeSpan.FromSeconds(30), cancellationToken);
        var executor = CreateExecutor();

        // act
        var outcome = await executor.ExecuteOpencodeServerAsync(
            actor, OpencodeSessionId, OpencodeServerUrl, null,
            attemptId, slot!.Value, FarFutureDeadline(), cancellationToken);

        // assert
        // the first health-only ping always writes, since there is
        // no prior recorded state to compare it against.
        Assert.Equal(AgentPingResult.Ok, outcome.Result);
        var row = await _agentStore.FindAsync(actor, cancellationToken);
        Assert.Equal(AgentPingResult.Ok, row!.LastPingResult);
        Assert.Equal(PingSessionExecutor.HealthOnlyDetail, row.LastPingDetail);
    }
}
