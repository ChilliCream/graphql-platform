using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using Mocha.Middlewares;
using Mocha.Transport.InMemory;

namespace Mocha.Sagas.Tests;

/// <summary>
/// Tests that a saga retried in-process resolves its store from the retry's own scope rather than
/// reusing the store instance the failed attempt cached on the saga feature.
/// </summary>
public class SagaRetryScopeTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Saga_Should_UseFreshStore_When_SaveIsRetried(bool warmPool)
    {
        // arrange
        var capture = new StoreCapture { FailOnSave = warmPool ? 2 : 1 };
        var pool = new TrackingReceiveContextPool();
        var services = new ServiceCollection();
        services.AddSingleton(capture);
        services.AddSingleton<ObjectPool<ReceiveContext>>(pool);
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
        ReceiveContext? warmupAttempt = null;
        if (warmPool)
        {
            await bus.PublishAsync(new StartRetrySaga(), CancellationToken.None);
            await pool.WaitForReceiveAsync();
            warmupAttempt = pool.LastAttempt;
        }

        await bus.PublishAsync(new StartRetrySaga(), CancellationToken.None);
        var receive = await pool.WaitForReceiveAsync();

        // assert
        if (warmPool)
        {
            Assert.Same(warmupAttempt, receive);
        }

        var expectedSaves = warmPool ? 3 : 2;
        var expectedStates = warmPool ? 2 : 1;
        Assert.Equal(expectedSaves, capture.StoreIds.Count);
        Assert.Equal(expectedSaves, capture.StoreIds.Distinct().Count());
        Assert.Equal(expectedStates, storage.Count);
        Assert.Equal(
            Enumerable.Repeat("Started", expectedStates),
            capture.SavedStates.Select(key => storage.Load<RetrySagaState>(key.SagaName, key.Id)?.State));
    }

    public sealed class StoreCapture
    {
        public ConcurrentQueue<Guid> StoreIds { get; } = new();

        public ConcurrentQueue<(string SagaName, Guid Id)> SavedStates { get; } = new();

        public int FailOnSave { get; init; }

        public int SaveCount;
    }

    /// <summary>
    /// Records scoped store use and fails one save.
    /// </summary>
    public sealed class ThrowOnceSagaStore(InMemorySagaStateStorage storage, StoreCapture capture)
        : ISagaStore, IDisposable
    {
        private readonly InMemorySagaStore _inner = new(storage);
        private readonly Guid _id = Guid.NewGuid();
        private bool _disposed;

        public Task<ISagaTransaction> StartTransactionAsync(CancellationToken cancellationToken)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _inner.StartTransactionAsync(cancellationToken);
        }

        public async Task SaveAsync<T>(Saga saga, T state, CancellationToken cancellationToken)
            where T : SagaStateBase
        {
            capture.StoreIds.Enqueue(_id);

            if (Interlocked.Increment(ref capture.SaveCount) == capture.FailOnSave)
            {
                throw new InvalidOperationException("transient");
            }

            await _inner.SaveAsync(saga, state, cancellationToken);
            capture.SavedStates.Enqueue((saga.Name, state.Id));
        }

        public Task DeleteAsync(Saga saga, Guid id, CancellationToken cancellationToken)
            => _inner.DeleteAsync(saga, id, cancellationToken);

        public Task<T?> LoadAsync<T>(Saga saga, Guid id, CancellationToken cancellationToken)
            => _inner.LoadAsync<T>(saga, id, cancellationToken);

        public void Dispose() => _disposed = true;
    }

    private sealed class TrackingReceiveContextPool : ObjectPool<ReceiveContext>
    {
        private readonly ReceiveContextPool _inner = new();
        private readonly Channel<ReceiveContext> _completed = Channel.CreateUnbounded<ReceiveContext>();
        private ReceiveContext? _receive;

        public ReceiveContext? LastAttempt { get; private set; }

        public override ReceiveContext Get()
        {
            var context = _inner.Get();
            if (_receive is null)
            {
                _receive = context;
            }
            else
            {
                LastAttempt = context;
            }

            return context;
        }

        public override void Return(ReceiveContext context)
        {
            var isReceive = ReferenceEquals(context, _receive);
            if (isReceive)
            {
                _receive = null;
            }

            _inner.Return(context);

            if (isReceive)
            {
                _completed.Writer.TryWrite(context);
            }
        }

        public async Task<ReceiveContext> WaitForReceiveAsync()
            => await _completed.Reader.ReadAsync(TestContext.Current.CancellationToken)
                .AsTask()
                .WaitAsync(s_timeout, TestContext.Current.CancellationToken);
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
