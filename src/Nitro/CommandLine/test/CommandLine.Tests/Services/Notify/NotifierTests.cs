using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Notify;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tests.Commands;
using ChilliCream.Nitro.CommandLine.Tests.Hook;
using Microsoft.Extensions.Time.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Exercises <see cref="Notifier"/>'s own thin contract: it dispatches each
/// distinct recipient actor exactly once through <see cref="IActorWakeDispatcher"/>,
/// never lets one recipient's dispatch failure stop the rest, and never
/// throws - the notifier contract every mail command relies on. The
/// direct-first state machine itself (claim, per-target dispatch, receipt
/// aggregation) is exercised in <see cref="ActorWakeDispatcherTests"/>; the
/// final test here wires the real <see cref="ActorWakeDispatcher"/> and
/// <see cref="PingSessionExecutor"/> together as an end-to-end smoke test.
/// </summary>
public sealed class NotifierTests
{
    [Fact]
    public async Task NotifyAsync_Should_DispatchEachDistinctActorOnce()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var dispatcher = new FakeActorWakeDispatcher();
        var notifier = new Notifier(dispatcher);

        // act
        await notifier.NotifyAsync(["agent-a", "agent-b", "agent-a"], cancellationToken);

        // assert: the duplicate "agent-a" collapses to a single dispatch.
        Assert.Equal(["agent-a", "agent-b"], dispatcher.DispatchedActors);
    }

    [Fact]
    public async Task NotifyAsync_Should_DispatchTheRemainingActors_When_OneActorsDispatchThrows()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var dispatcher = new FakeActorWakeDispatcher { ThrowingActor = "agent-a" };
        var notifier = new Notifier(dispatcher);

        // act & assert: never throws, and still reaches "agent-b".
        await notifier.NotifyAsync(["agent-a", "agent-b"], cancellationToken);
        Assert.Equal(["agent-a", "agent-b"], dispatcher.DispatchedActors);
    }

    [Fact]
    public async Task NotifyAsync_Should_NeverThrow_When_TheRecipientListIsEmpty()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var notifier = new Notifier(new FakeActorWakeDispatcher());

        // act & assert
        await notifier.NotifyAsync([], cancellationToken);
    }

    [Fact]
    public async Task NotifyAsync_Should_DeliverEndToEnd_When_WiredToTheRealDispatcher()
    {
        // arrange: the real ActorWakeDispatcher, SessionGateCoordinator, and
        // PingSessionExecutor wired together against a real workspace
        // database, with only the outermost Codex transport faked - proves
        // the notifier's DI shape actually dispatches a wake end to end.
        var cancellationToken = TestContext.Current.CancellationToken;
        const string actor = "codex-worker";
        var tempRoot = Directory.CreateTempSubdirectory("nitro-notifier-e2e-tests");

        try
        {
            var workspaceDirectory = AgentWorkspace.GetDirectory(tempRoot.FullName);
            Directory.CreateDirectory(workspaceDirectory);
            var fileSystem = new TestFileSystem(tempRoot.FullName);
            var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero));
            var database = new AgentDatabase();
            var agentRegistry = new AgentRegistry(fileSystem, timeProvider, database);
            var sessions = new AgentSessionRegistry(
                fileSystem,
                timeProvider,
                database,
                agentRegistry,
                new FixedInstanceIdProvider("host-1"),
                new FixedGlobalConfigDirectoryProvider(tempRoot.FullName),
                new ProcessInfoProvider(),
                new FixedAncestorSessionResolver(null));
            var instanceIdProvider = new FixedInstanceIdProvider("host-1");
            var globalConfigDirectoryProvider = new FixedGlobalConfigDirectoryProvider(tempRoot.FullName);
            var mail = new MailStore(
                fileSystem, timeProvider, database, agentRegistry, instanceIdProvider, globalConfigDirectoryProvider);
            var batches = new MailWakeBatchStore(fileSystem, database);
            var gates = new SessionPingGateStore(fileSystem, database);
            var leases = new PingLeaseStore(fileSystem, database);
            var gateCoordinator = new SessionGateCoordinator(gates, leases);
            var queueClient = new FakeCodexQueueClient();
            var executor = new PingSessionExecutor(
                mail, queueClient, new NoopClaudePeerClient(), sessions, leases, timeProvider);
            var dispatcher = new ActorWakeDispatcher(
                batches,
                sessions,
                gateCoordinator,
                executor,
                mail,
                new FixedInstanceIdProvider("host-1"),
                new FixedGlobalConfigDirectoryProvider(tempRoot.FullName),
                timeProvider);
            var notifier = new Notifier(dispatcher);

            await using (await database.InitializeAsync(workspaceDirectory, cancellationToken))
            {
            }

            var pid = Environment.ProcessId;
            var procStart = new ProcessInfoProvider().GetStartTicks(pid)!;
            var generation = new AgentSessionGeneration(
                AgentSessionHarness.Codex, "session-1", "host-1", pid, procStart);

            await sessions.StartAsync(
                generation, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.CodexThread, "thread-1",
                envActor: actor, cancellationToken);

            var message = await mail.SendMessageAsync(
                new MailMessageCreation
                {
                    Sender = "pascal",
                    Subject = "status",
                    Body = "check",
                    To = [actor],
                    WakePolicy = MailWakePolicy.Enqueue
                },
                cancellationToken);

            // act
            await notifier.NotifyAsync(
                message.Recipients.Select(r => r.Name).ToArray(), cancellationToken);

            // assert
            var call = Assert.Single(queueClient.Calls);
            Assert.Equal("thread-1", call.ThreadId);
            Assert.Contains(message.Id, call.Message);
        }
        finally
        {
            tempRoot.Delete(recursive: true);
        }
    }
}

internal sealed class FakeActorWakeDispatcher : IActorWakeDispatcher
{
    public List<string> DispatchedActors { get; } = [];

    public string? ThrowingActor { get; set; }

    public Task<ActorWakeReceipt?> DispatchAsync(string actor, CancellationToken cancellationToken)
    {
        DispatchedActors.Add(actor);

        if (actor == ThrowingActor)
        {
            throw new InvalidOperationException($"Simulated dispatch failure for '{actor}'.");
        }

        return Task.FromResult<ActorWakeReceipt?>(new ActorWakeReceipt(actor, MailWakeTargetStatus.Delivered, []));
    }
}

/// <summary>
/// Never reached by the codex-thread end-to-end smoke test, but required to
/// satisfy <see cref="PingSessionExecutor"/>'s constructor.
/// </summary>
internal sealed class NoopClaudePeerClient : IClaudePeerClient
{
    public Task<ClaudePeerSendOutcome> SendAsync(
        int pid, string sessionId, string message, CancellationToken cancellationToken)
        => Task.FromResult(ClaudePeerSendOutcome.Ok);
}
