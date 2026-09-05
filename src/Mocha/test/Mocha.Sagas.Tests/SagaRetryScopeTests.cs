using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.InMemory;

namespace Mocha.Sagas.Tests;

/// <summary>
/// Tests that a saga retried in-process resolves its store from the retry's own scope rather than
/// reusing the store instance the failed attempt cached on the saga feature.
/// </summary>
public class SagaRetryScopeTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task Saga_Should_UseFreshStore_When_SaveIsRetried()
    {
        // arrange
        var capture = new StoreCapture();
        var services = new ServiceCollection();
        services.AddSingleton(capture);
        // registered first so the in-memory registration's TryAdd keeps this store
        services.AddScoped<ISagaStore, ThrowOnceSagaStore>();
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

        // act
        await bus.PublishAsync(new StartRetrySaga(), CancellationToken.None);
        await capture.Saved.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        // the save is staged in the saga transaction and reaches storage when the attempt commits
        var deadline = DateTime.UtcNow + s_timeout;
        while (storage.Count == 0 && DateTime.UtcNow < deadline)
        {
            await Task.Delay(25, TestContext.Current.CancellationToken);
        }

        // assert - the failed save and the retried save came from two different scoped stores
        Assert.Equal(2, capture.StoreIds.Count);
        Assert.Equal(2, capture.StoreIds.Distinct().Count());
        Assert.Equal(1, storage.Count);
    }

    public sealed class StoreCapture
    {
        public ConcurrentQueue<Guid> StoreIds { get; } = new();

        public TaskCompletionSource Saved { get; } = new();

        public int Failures;
    }

    /// <summary>
    /// Wraps the in-memory store, records which instance each save came from, and fails the first.
    /// </summary>
    public sealed class ThrowOnceSagaStore(InMemorySagaStateStorage storage, StoreCapture capture) : ISagaStore
    {
        private readonly InMemorySagaStore _inner = new(storage);
        private readonly Guid _id = Guid.NewGuid();

        public Task<ISagaTransaction> StartTransactionAsync(CancellationToken cancellationToken)
            => _inner.StartTransactionAsync(cancellationToken);

        public async Task SaveAsync<T>(Saga saga, T state, CancellationToken cancellationToken)
            where T : SagaStateBase
        {
            capture.StoreIds.Enqueue(_id);

            if (Interlocked.Increment(ref capture.Failures) == 1)
            {
                throw new InvalidOperationException("transient");
            }

            await _inner.SaveAsync(saga, state, cancellationToken);
            capture.Saved.TrySetResult();
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

            descriptor.During("Started").OnEvent<EndRetrySaga>().TransitionTo("Ended");

            descriptor.Finally("Ended");
        }
    }
}
