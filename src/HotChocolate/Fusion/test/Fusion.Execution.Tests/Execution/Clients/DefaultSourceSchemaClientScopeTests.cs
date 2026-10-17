using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using HotChocolate.Features;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution;

public sealed class DefaultSourceSchemaClientScopeTests : FusionTestBase
{
    [Fact]
    public async Task GetClient_Should_ReuseEachKey_When_NamesAndOperationTypesDiffer()
    {
        // arrange
        var factory = new RecordingFactory();
        await using var scope = CreateScope(factory);
        var keys = new[]
        {
            ("A", OperationType.Query), ("A", OperationType.Mutation),
            ("B", OperationType.Query), ("C", OperationType.Subscription)
        };

        // act
        var clients = keys.Select(key => scope.GetClient(key.Item1, key.Item2)).ToArray();
        var repeated = keys.Select(key => scope.GetClient(key.Item1, key.Item2)).ToArray();

        // assert
        Assert.Equal(4, factory.CreatedCount);
        Assert.Equal(4, clients.Distinct(ReferenceEqualityComparer.Instance).Count());
        Assert.Equal(clients, repeated);
    }

    [Fact]
    public async Task GetClient_Should_CreateOncePerKey_When_CalledConcurrently()
    {
        // arrange
        var factory = new RecordingFactory();
        await using var scope = CreateScope(factory);
        var keys = new[]
        {
            ("A", OperationType.Query), ("A", OperationType.Mutation),
            ("B", OperationType.Query), ("C", OperationType.Subscription)
        };

        // act
        var clients = await Task.WhenAll(Enumerable.Range(0, 64).Select(index => Task.Run(
            () => scope.GetClient(keys[index % 4].Item1, keys[index % 4].Item2),
            TestContext.Current.CancellationToken)));

        // assert
        Assert.Equal(4, factory.CreatedCount);
        Assert.All(Enumerable.Range(0, clients.Length), index =>
            Assert.Same(scope.GetClient(keys[index % 4].Item1, keys[index % 4].Item2), clients[index]));
    }

    [Fact]
    public async Task ResetAsync_Should_KeepSnapshot_When_DisposalCreatesAnotherClient()
    {
        // arrange
        var factory = new RecordingFactory();
        await using var scope = CreateScope(factory);
        var first = (RecordingClient)scope.GetClient("A", OperationType.Query);
        var fallback = (RecordingClient)scope.GetClient("B", OperationType.Query);
        RecordingClient? callbackClient = null;
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        first.OnDispose = async () =>
        {
            callbackClient = (RecordingClient)scope.GetClient("C", OperationType.Query);
            started.TrySetResult();
            await release.Task;
        };

        // act
        var reset = scope.ResetAsync();
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        var pendingState = new[]
        {
            reset.IsCompleted,
            ReferenceEquals(first, scope.GetClient("A", OperationType.Query)),
            ReferenceEquals(fallback, scope.GetClient("B", OperationType.Query))
        };
        release.SetResult();
        await reset;
        var nextClient = scope.GetClient("C", OperationType.Query);

        // assert
        Assert.Equal([false, true, true], pendingState);
        Assert.Equal<int>([1, 1, 0], [first.Disposals, fallback.Disposals, callbackClient!.Disposals]);
        Assert.NotSame(callbackClient, nextClient);
    }

    [Theory]
    [InlineData("A")]
    [InlineData("B")]
    public async Task ResetAsync_Should_KeepEntries_When_ClientDisposalThrows(string failureKey)
    {
        // arrange
        var factory = new RecordingFactory();
        await using var scope = CreateScope(factory);
        var first = (RecordingClient)scope.GetClient("A", OperationType.Query);
        var fallback = (RecordingClient)scope.GetClient("B", OperationType.Query);
        var failing = failureKey == "A" ? first : fallback;
        failing.OnDispose = () => ValueTask.FromException(new DisposalFailureException());

        // act
        var failure = await Record.ExceptionAsync(() => scope.ResetAsync().AsTask());
        var retained = new[]
        {
            ReferenceEquals(first, scope.GetClient("A", OperationType.Query)),
            ReferenceEquals(fallback, scope.GetClient("B", OperationType.Query))
        };
        failing.OnDispose = null;
        await scope.ResetAsync();
        var replaced = new[]
        {
            ReferenceEquals(first, scope.GetClient("A", OperationType.Query)),
            ReferenceEquals(fallback, scope.GetClient("B", OperationType.Query))
        };

        // assert
        Assert.IsType<DisposalFailureException>(failure);
        Assert.Equal([true, true], retained);
        Assert.Equal([false, false], replaced);
    }

    [Fact]
    public async Task GetClient_Should_Complete_When_FactoryResetsClientOnAnotherThread()
    {
        // arrange
        var factory = new RecordingFactory();
        var scope = CreateScope(factory);
        var first = (RecordingClient)scope.GetClient("B", OperationType.Query);
        first.OnDispose = async () => await Task.Yield();
        factory.OnCreated = client =>
        {
            if (client.Name == "A")
            {
                Task.Run(async () => await scope.ResetAsync()).GetAwaiter().GetResult();
            }
        };

        // act
        var created = await Task.Run(
                () => scope.GetClient("A", OperationType.Query),
                TestContext.Current.CancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(1, first.Disposals);
        Assert.Same(created, scope.GetClient("A", OperationType.Query));
        Assert.Equal(2, factory.CreatedCount);
        await scope.DisposeAsync();
    }

    [Fact]
    public async Task GetClient_Should_PreserveCachedClient_When_FactoryReentersSameKey()
    {
        // arrange
        var factory = new RecordingFactory();
        await using var scope = CreateScope(factory);
        RecordingClient? inner = null;
        factory.OnCreated = _ =>
        {
            if (factory.CreatedCount == 1)
            {
                inner = (RecordingClient)scope.GetClient("A", OperationType.Query);
            }
        };

        // act
        var outer = (RecordingClient)scope.GetClient("A", OperationType.Query);
        var cached = scope.GetClient("A", OperationType.Query);
        await scope.ResetAsync();

        // assert
        Assert.Equal(2, factory.CreatedCount);
        Assert.NotSame(outer, inner);
        Assert.Same(inner, cached);
        Assert.Equal<int>([0, 1], [outer.Disposals, inner!.Disposals]);
    }

    [Fact]
    public async Task DisposeAsync_Should_DisposeEachClientOnce_When_CalledRepeatedly()
    {
        // arrange
        var factory = new RecordingFactory();
        var scope = CreateScope(factory);
        var first = (RecordingClient)scope.GetClient("A", OperationType.Query);
        var fallback = (RecordingClient)scope.GetClient("B", OperationType.Query);

        // act
        await scope.DisposeAsync();
        await scope.DisposeAsync();
        await scope.ResetAsync();

        // assert
        Assert.Equal<int>([1, 1], [first.Disposals, fallback.Disposals]);
    }

    [Fact]
    public void ResetAsync_Should_ReleaseClientReference_When_ResetCompletes()
    {
        // arrange
        var (scope, client) = CreateResetScope();

        // act
        GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);

        // assert
        Assert.False(client.IsAlive);
        GC.KeepAlive(scope);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (DefaultSourceSchemaClientScope Scope, WeakReference Client) CreateResetScope()
    {
        var scope = CreateScope(new RecordingFactory());
        var client = scope.GetClient("A", OperationType.Query);
        var reference = new WeakReference(client);
        scope.ResetAsync().GetAwaiter().GetResult();
        return (scope, reference);
    }

    private static DefaultSourceSchemaClientScope CreateScope(RecordingFactory factory)
    {
        var features = new FeatureCollection();
        features.Set(new SourceSchemaClientConfigurations(
            [new ClientConfiguration("A"), new ClientConfiguration("B"), new ClientConfiguration("C")]));
        var schema = FusionSchemaDefinition.Create(
            ComposeSchemaDocument("type Query { value: String }"),
            features: features);
        return new DefaultSourceSchemaClientScope(schema, [factory]);
    }

    private sealed class ClientConfiguration(string name) : ISourceSchemaClientConfiguration
    {
        public string Name { get; } = name;

        public SupportedOperationType SupportedOperations => SupportedOperationType.All;
    }

    private sealed class RecordingFactory : ISourceSchemaClientFactory
    {
        private int _createdCount;

        public int CreatedCount => Volatile.Read(ref _createdCount);

        public Action<RecordingClient>? OnCreated { get; set; }

        public bool CanHandle(ISourceSchemaClientConfiguration configuration)
            => configuration is ClientConfiguration;

        public ISourceSchemaClient CreateClient(
            FusionSchemaDefinition schema,
            ISourceSchemaClientConfiguration configuration)
        {
            Interlocked.Increment(ref _createdCount);
            var client = new RecordingClient(configuration.Name);
            OnCreated?.Invoke(client);
            return client;
        }
    }

    private sealed class RecordingClient(string name) : ISourceSchemaClient
    {
        public string Name { get; } = name;

        public SourceSchemaClientCapabilities Capabilities => SourceSchemaClientCapabilities.Default;

        public Func<ValueTask>? OnDispose { get; set; }

        public int Disposals { get; private set; }

        public ValueTask DisposeAsync()
        {
            Disposals++;
            return OnDispose?.Invoke() ?? ValueTask.CompletedTask;
        }

        public IAsyncEnumerable<SourceSchemaResult> ExecuteAsync(
            OperationPlanContext context,
            SourceSchemaClientRequest request,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public IAsyncEnumerable<SourceSchemaBatchResult> ExecuteBatchAsync(
            OperationPlanContext context,
            ImmutableArray<SourceSchemaClientRequest> requests,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public IAsyncEnumerable<SourceSchemaResult> SubscribeAsync(
            OperationPlanContext context,
            SourceSchemaClientRequest request,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();
    }

    private sealed class DisposalFailureException : Exception;
}
