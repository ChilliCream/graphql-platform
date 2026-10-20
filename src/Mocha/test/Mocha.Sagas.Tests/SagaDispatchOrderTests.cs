using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.InMemory;

namespace Mocha.Sagas.Tests;

/// <summary>
/// Tests that a saga's state is durable before the messages of a transition leave the bus, so a
/// reply cannot be consumed before the state that handles it exists.
/// </summary>
public class SagaDispatchOrderTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

    private static readonly TimeSpan s_raceWindow = TimeSpan.FromSeconds(2);

    [Fact]
    public async Task Saga_Should_HandleReply_When_SaveOfSendingAttemptIsSlow()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var probe = new ReplyRaceProbe();
        var services = new ServiceCollection();
        services.AddSingleton(probe);
        // registered first so the in-memory registration's TryAdd keeps this store
        services.AddScoped<ISagaStore, ReplyRaceSagaStore>();
        services.AddInMemorySagas();
        var builder = services.AddMessageBus().AddSaga<LookupSaga>().AddRequestHandler<LookupHandler>();
        // queues process in order, so a probe sent after the saga's command is handled after it
        builder.AddInMemory(t => t.AddConvention(new SingleWorkerConvention()));

        await using var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(CancellationToken.None);
        var storage = provider.GetRequiredService<InMemorySagaStateStorage>();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        var request = bus.RequestAsync(new StartLookup("ada"), ct).AsTask();
        // the saga is about to save its state, so a command that left the bus already has been handled
        // by the time the probe is
        await probe.Saving.Task.WaitAsync(s_timeout, ct);
        await bus.SendAsync(new Lookup("probe"), ct);
        await probe.ProbeHandled.Task.WaitAsync(s_timeout, ct);
        probe.ProceedWithSave.SetResult();
        var completed = await Task.WhenAny(request, probe.ReplyFoundNoState.Task).WaitAsync(s_timeout, ct);

        // assert
        Assert.True(
            ReferenceEquals(request, completed),
            "the reply was consumed before the saga state was persisted");
        Assert.Equal("ada", (await request).Key);
        Assert.Equal(0, storage.Count);
    }

    public sealed class ReplyRaceProbe
    {
        public bool CommandHandled { get; set; }

        public TaskCompletionSource Saving { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource ProceedWithSave { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource ProbeHandled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource ReplyFoundNoState { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    /// <summary>
    /// Holds the first save until the test releases it. When the saga's command was handled before
    /// the save, the save also waits until the reply has been consumed, which is the window the race
    /// needs. That wait is bounded, so a saga whose command leaves after the save pays nothing.
    /// </summary>
    public sealed class ReplyRaceSagaStore(InMemorySagaStateStorage storage, ReplyRaceProbe probe) : ISagaStore
    {
        private readonly InMemorySagaStore _inner = new(storage);

        public Task<ISagaTransaction> StartTransactionAsync(CancellationToken cancellationToken)
            => _inner.StartTransactionAsync(cancellationToken);

        public async Task SaveAsync<T>(Saga saga, T state, CancellationToken cancellationToken)
            where T : SagaStateBase
        {
            probe.Saving.TrySetResult();
            await probe.ProceedWithSave.Task.WaitAsync(s_timeout, cancellationToken);

            if (probe.CommandHandled)
            {
                try
                {
                    await probe.ReplyFoundNoState.Task.WaitAsync(s_raceWindow, cancellationToken);
                }
                catch (TimeoutException)
                {
                    // the reply found the state, so there is no race to expose
                }
            }

            await _inner.SaveAsync(saga, state, cancellationToken);
        }

        public Task DeleteAsync(Saga saga, Guid id, CancellationToken cancellationToken)
            => _inner.DeleteAsync(saga, id, cancellationToken);

        public async Task<T?> LoadAsync<T>(Saga saga, Guid id, CancellationToken cancellationToken)
        {
            var state = await _inner.LoadAsync<T>(saga, id, cancellationToken);

            if (state is null)
            {
                probe.ReplyFoundNoState.TrySetResult();
            }

            return state;
        }
    }

    public sealed class LookupState : SagaStateBase
    {
        public required string Key { get; init; }
    }

    public sealed record StartLookup(string Key) : IEventRequest<LookupCompleted>;

    public sealed record LookupCompleted(string Key);

    public sealed record Lookup(string Key) : IEventRequest<LookupResult>;

    public sealed record LookupResult(string Key);

    public sealed class LookupHandler(ReplyRaceProbe probe) : IEventRequestHandler<Lookup, LookupResult>
    {
        public ValueTask<LookupResult> HandleAsync(Lookup request, CancellationToken cancellationToken)
        {
            if (request.Key == "probe")
            {
                probe.ProbeHandled.TrySetResult();
            }
            else
            {
                probe.CommandHandled = true;
            }

            return new(new LookupResult(request.Key));
        }
    }

    /// <summary>
    /// Processes every queue with one worker, so the messages of a queue are handled in the order
    /// they arrived.
    /// </summary>
    private sealed class SingleWorkerConvention : IInMemoryReceiveEndpointConfigurationConvention
    {
        public void Configure(
            IMessagingConfigurationContext context,
            InMemoryMessagingTransport transport,
            InMemoryReceiveEndpointConfiguration configuration)
        {
            configuration.MaxConcurrency = 1;
        }
    }

    public sealed class LookupSaga : Saga<LookupState>
    {
        protected override void Configure(ISagaDescriptor<LookupState> descriptor)
        {
            descriptor
                .Initially()
                .OnRequest<StartLookup>()
                .StateFactory(r => new LookupState { Key = r.Key })
                .Send(s => new Lookup(s.Key))
                .TransitionTo("Awaiting");

            descriptor.During("Awaiting").OnReply<LookupResult>().TransitionTo("Done");

            descriptor.Finally("Done").Respond(s => new LookupCompleted(s.Key));
        }
    }
}
