using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.InMemory;

namespace Mocha.Sagas.Tests;

/// <summary>
/// Same as <see cref="SagaRetryScopeTests"/>, but a saga consume has already run on the bus, so
/// the receive context rented for the retried message is a pooled instance that served as a saga
/// attempt before and carries a restored <see cref="SagaFeature"/>.
/// </summary>
public class SagaRetryScopeWarmupTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task Saga_Should_UseFreshStore_When_SaveIsRetried_After_EarlierSagaConsume()
    {
        // arrange
        var capture = new StoreCapture();
        var services = new ServiceCollection();
        services.AddSingleton(capture);
        services.AddScoped<ISagaStore, ArmedThrowOnceSagaStore>();
        services.AddInMemorySagas();
        var builder = services.AddMessageBus()
            .AddResilience(p => p.On<Exception>().Retry(3, TimeSpan.FromMilliseconds(1), RetryBackoffType.Constant))
            .AddSaga<RetrySaga>();
        builder.AddInMemory();

        await using var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(CancellationToken.None);

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var storage = provider.GetRequiredService<InMemorySagaStateStorage>();

        // warm-up: one saga consume that succeeds
        await bus.PublishAsync(new StartRetrySaga(), CancellationToken.None);
        await WaitUntilAsync(() => storage.Count == 1);

        // act: a second saga instance whose first save fails once
        capture.Arm();
        await bus.PublishAsync(new StartRetrySaga(), CancellationToken.None);
        await capture.Saved.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        await WaitUntilAsync(() => storage.Count == 2);

        // assert - the failed save and the retried save must come from two different scoped stores
        Assert.Equal(2, capture.ArmedStoreIds.Count);
        Assert.Equal(2, capture.ArmedStoreIds.Distinct().Count());
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow + s_timeout;
        while (!condition() && DateTime.UtcNow < deadline)
        {
            await Task.Delay(25, TestContext.Current.CancellationToken);
        }

        Assert.True(condition(), "timed out waiting for the saga state to land");
    }

    public sealed class StoreCapture
    {
        private int _armed;

        public ConcurrentQueue<Guid> ArmedStoreIds { get; } = new();

        public TaskCompletionSource Saved { get; } = new();

        public int Failures;

        public bool Armed => Volatile.Read(ref _armed) == 1;

        public void Arm() => Volatile.Write(ref _armed, 1);
    }

    public sealed class ArmedThrowOnceSagaStore(InMemorySagaStateStorage storage, StoreCapture capture) : ISagaStore
    {
        private readonly InMemorySagaStore _inner = new(storage);
        private readonly Guid _id = Guid.NewGuid();

        public Task<ISagaTransaction> StartTransactionAsync(CancellationToken cancellationToken)
            => _inner.StartTransactionAsync(cancellationToken);

        public async Task SaveAsync<T>(Saga saga, T state, CancellationToken cancellationToken)
            where T : SagaStateBase
        {
            if (capture.Armed)
            {
                capture.ArmedStoreIds.Enqueue(_id);

                if (Interlocked.Increment(ref capture.Failures) == 1)
                {
                    throw new InvalidOperationException("transient");
                }
            }

            await _inner.SaveAsync(saga, state, cancellationToken);

            if (capture.Armed)
            {
                capture.Saved.TrySetResult();
            }
        }

        public Task DeleteAsync(Saga saga, Guid id, CancellationToken cancellationToken)
            => _inner.DeleteAsync(saga, id, cancellationToken);

        public Task<T?> LoadAsync<T>(Saga saga, Guid id, CancellationToken cancellationToken)
            => _inner.LoadAsync<T>(saga, id, cancellationToken);
    }

    public sealed class RetrySagaState : SagaStateBase;

    public sealed class StartRetrySaga;

    public sealed class EndRetrySaga;

    public sealed class RetrySaga : Saga<RetrySagaState>
    {
        protected override void Configure(ISagaDescriptor<RetrySagaState> descriptor)
        {
            descriptor
                .Initially()
                .OnEvent<StartRetrySaga>()
                .StateFactory(_ => new RetrySagaState())
                .TransitionTo("Started");

            // Started is not final, so the state stays in storage and Count is observable.
            descriptor.During("Started").OnEvent<EndRetrySaga>().TransitionTo("Ended");

            descriptor.Finally("Ended");
        }
    }
}
