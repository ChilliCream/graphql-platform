using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Diagnostics;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution;

public sealed class OperationExecutionNodeTests : FusionTestBase
{
    [Fact]
    public async Task ExecuteAsync_Should_Succeed_When_SourceSchemaReturnsNoResults()
    {
        // arrange
        var client = new EmptyQueryClient();
        var executor = await CreateExecutorAsync(client, SupportedOperationType.Query);
        var request = CreateQueryRequest();

        // act
        await using var result = await executor.ExecuteAsync(
            request,
            TestContext.Current.CancellationToken);

        // assert
        var operationResult = result.ExpectOperationResult();
        Assert.True(operationResult.Data.HasValue);

        if (operationResult.Errors is { } errors)
        {
            Assert.Empty(errors);
        }
    }

    [Fact]
    public async Task MoveNextAsync_Should_DisposeEventArena_When_EventFailsAfterArenaMinted()
    {
        // arrange
        var client = new ThrowingSubscriptionClient();
        var executor = await CreateExecutorAsync(client);
        var request = CreateSubscriptionRequest();

        // act
        await using var result = await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);
        var stream = result.ExpectResponseStream();
        await using var enumerator = stream
            .ReadResultsAsync()
            .GetAsyncEnumerator(TestContext.Current.CancellationToken);

        var hasResult = await enumerator.MoveNextAsync();
        Assert.True(hasResult);
        var errorResult = enumerator.Current;
        var mintedArena = client.MintedArenas.Single();
        var hasNextResult = await enumerator.MoveNextAsync();

        // assert
        try
        {
            // the failure surfaces as one terminal error result and then the stream ends;
            // the arena minted during the failed iteration was never bound, so the enumerator owns and releases it
            var arena = (MemoryArena)mintedArena;
            var exception = errorResult.Errors?.Single().Exception;

            Snapshot.Create()
                .Add(errorResult.ToJson(), "Terminal Error Result", MarkdownLanguages.Json)
                .Add(
                    new
                    {
                        ExceptionType = exception?.GetType().Name,
                        ExceptionMessage = exception?.Message,
                        StreamEnded = !hasNextResult,
                        ArenaRentExceptionType = Record.Exception(() => arena.Rent(1))?.GetType().Name,
                        ArenaRentedPageCount = arena.RentedPageCount
                    },
                    "Stream And Arena State")
                .MatchMarkdownSnapshot();
        }
        finally
        {
            await errorResult.DisposeAsync();
        }
    }

    [Fact]
    public async Task MoveNextAsync_Should_NotDisposePriorEventArena_When_NextEventFailsBeforeArenaMinted()
    {
        // arrange
        var client = new YieldThenThrowBeforeMintingSubscriptionClient();
        var executor = await CreateExecutorAsync(client);
        var request = CreateSubscriptionRequest();

        // act
        await using var result = await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);
        var stream = result.ExpectResponseStream();
        await using var enumerator = stream
            .ReadResultsAsync()
            .GetAsyncEnumerator(TestContext.Current.CancellationToken);

        var hasFirstResult = await enumerator.MoveNextAsync();
        Assert.True(hasFirstResult);
        var firstResult = enumerator.Current;
        var mintedArena = client.MintedArenas.Single();
        var hasSecondResult = await enumerator.MoveNextAsync();
        Assert.True(hasSecondResult);
        var secondResult = enumerator.Current;
        var hasThirdResult = await enumerator.MoveNextAsync();

        // assert
        try
        {
            // the delivered event arrives, then one terminal error result, then the stream ends;
            // the arena bound to the delivered event is still owned by that result
            var firstArena = (MemoryArena)mintedArena;
            var exception = secondResult.Errors?.Single().Exception;

            Snapshot.Create()
                .Add(secondResult.ToJson(), "Terminal Error Result", MarkdownLanguages.Json)
                .Add(
                    new
                    {
                        ExceptionType = exception?.GetType().Name,
                        ExceptionMessage = exception?.Message,
                        StreamEnded = !hasThirdResult,
                        FirstEventArenaDisposed = firstArena.IsDisposed
                    },
                    "Stream And Arena State")
                .MatchMarkdownSnapshot();
        }
        finally
        {
            await firstResult.DisposeAsync();
            await secondResult.DisposeAsync();
        }
    }

    [Fact]
    public async Task Subscription_Should_BorrowDedicatedScope_ForSameSchemaDependentClient()
    {
        // arrange
        var clientScopeFactory = new TestClientScopeFactory();
        var executor = await CreateExecutorAsync(
            new EmptyQueryClient(),
            clientScopeFactory: clientScopeFactory);
        var request = CreateSubscriptionRequest();

        // act
        await using var result = await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);
        var stream = result.ExpectResponseStream();
        await using var enumerator = stream
            .ReadResultsAsync()
            .GetAsyncEnumerator(TestContext.Current.CancellationToken);

        var hasResult = await enumerator.MoveNextAsync();

        // assert
        Assert.False(hasResult);
        Assert.Equal(2, clientScopeFactory.Scopes.Count);
        var subscriptionClient = (ScopeBorrowSubscriptionClient)clientScopeFactory.Scopes[1].Client;
        Assert.Same(subscriptionClient, subscriptionClient.DependentClient);
        Assert.Equal(1, clientScopeFactory.Scopes[1].DisposeCount);
    }

    [Fact]
    public async Task Subscription_Should_DisposeDedicatedScope_When_SubscriptionFails()
    {
        // arrange
        var clientScopeFactory = new TestClientScopeFactory(
            static () => new ThrowingScopeBorrowSubscriptionClient());
        var executor = await CreateExecutorAsync(
            new EmptyQueryClient(),
            clientScopeFactory: clientScopeFactory);
        var request = CreateSubscriptionRequest();

        // act
        await using var result = await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);
        var stream = result.ExpectResponseStream();
        await using var enumerator = stream
            .ReadResultsAsync()
            .GetAsyncEnumerator(TestContext.Current.CancellationToken);

        var hasResult = await enumerator.MoveNextAsync();
        var errorResult = enumerator.Current;

        // assert
        try
        {
            Assert.True(hasResult);
            Assert.Single(errorResult.Errors!);
            Assert.Equal(1, clientScopeFactory.Scopes[1].DisposeCount);
        }
        finally
        {
            await errorResult.DisposeAsync();
        }
    }

    [Fact]
    public async Task Subscription_Should_PreserveSetupError_WhenCleanupFails()
    {
        // arrange
        var setupException = new InvalidOperationException("The subscription setup failed.");
        var diagnosticListener = new CleanupDiagnosticListener();
        var clientScopeFactory = new TestClientScopeFactory(
            getClientException: setupException,
            disposeException: new InvalidOperationException("The client scope cleanup failed."));
        var executor = await CreateExecutorAsync(
            new EmptyQueryClient(),
            clientScopeFactory: clientScopeFactory,
            diagnosticListener: diagnosticListener);

        // act
        await using var result = await executor.ExecuteAsync(
            CreateSubscriptionRequest(),
            TestContext.Current.CancellationToken);
        var stream = result.ExpectResponseStream();
        await using var enumerator = stream.ReadResultsAsync().GetAsyncEnumerator(TestContext.Current.CancellationToken);
        var hasResult = await enumerator.MoveNextAsync();
        var error = enumerator.Current.Errors!.Single().Exception;

        // assert
        Assert.True(hasResult);
        Assert.Same(setupException, error);
        Assert.Equal(1, diagnosticListener.SubscriptionScope.DisposeCount);
        Assert.Equal(1, clientScopeFactory.Scopes[1].DisposeCount);
    }

    [Fact]
    public async Task Subscription_Should_PreserveMoveNextError_WhenCleanupFails()
    {
        // arrange
        var moveNextException = new InvalidOperationException("The subscription read failed.");
        var client = new ThrowingMoveNextSubscriptionClient(moveNextException);
        var diagnosticListener = new CleanupDiagnosticListener();
        var clientScopeFactory = new TestClientScopeFactory(
            () => client,
            disposeException: new InvalidOperationException("The client scope cleanup failed."));
        var executor = await CreateExecutorAsync(
            new EmptyQueryClient(),
            clientScopeFactory: clientScopeFactory,
            diagnosticListener: diagnosticListener);

        // act
        await using var result = await executor.ExecuteAsync(
            CreateSubscriptionRequest(),
            TestContext.Current.CancellationToken);
        var stream = result.ExpectResponseStream();
        await using var enumerator = stream.ReadResultsAsync().GetAsyncEnumerator(TestContext.Current.CancellationToken);
        var hasResult = await enumerator.MoveNextAsync();
        var error = enumerator.Current.Errors!.Single().Exception;

        // assert
        Assert.True(hasResult);
        Assert.Same(moveNextException, error);
        Assert.Equal(1, client.DisposeCount);
        Assert.Equal(1, diagnosticListener.SubscriptionScope.DisposeCount);
        Assert.Equal(1, clientScopeFactory.Scopes[1].DisposeCount);
    }

    [Fact]
    public async Task Subscription_Should_Complete_WhenCancellationCleanupFails()
    {
        // arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var client = new CancelledSubscriptionClient(cancellationTokenSource);
        var clientScopeFactory = new TestClientScopeFactory(
            () => client,
            disposeException: new InvalidOperationException("The client scope cleanup failed."));
        var executor = await CreateExecutorAsync(
            new EmptyQueryClient(),
            clientScopeFactory: clientScopeFactory);

        // act
        await using var result = await executor.ExecuteAsync(
            CreateSubscriptionRequest(),
            TestContext.Current.CancellationToken);
        var stream = result.ExpectResponseStream();
        await using var enumerator = stream.ReadResultsAsync().GetAsyncEnumerator(cancellationTokenSource.Token);
        var hasResult = await enumerator.MoveNextAsync();

        // assert
        Assert.False(hasResult);
        Assert.Equal(1, client.DisposeCount);
        Assert.Equal(1, clientScopeFactory.Scopes[1].DisposeCount);
    }

    [Fact]
    public async Task Subscription_Should_ThrowFirstCleanupError_WhenStreamCompletes()
    {
        // arrange
        var client = new CompletingSubscriptionClient();
        var diagnosticListener = new CleanupDiagnosticListener();
        var clientScopeFactory = new TestClientScopeFactory(
            () => client,
            disposeException: new InvalidOperationException("The client scope cleanup failed."));
        var executor = await CreateExecutorAsync(
            new EmptyQueryClient(),
            clientScopeFactory: clientScopeFactory,
            diagnosticListener: diagnosticListener);

        // act
        await using var result = await executor.ExecuteAsync(
            CreateSubscriptionRequest(),
            TestContext.Current.CancellationToken);
        var stream = result.ExpectResponseStream();
        await using var enumerator = stream.ReadResultsAsync().GetAsyncEnumerator(TestContext.Current.CancellationToken);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await enumerator.MoveNextAsync());

        // assert
        Assert.Equal("The event enumerator cleanup failed.", exception.Message);
        Assert.Equal(1, client.DisposeCount);
        Assert.Equal(1, diagnosticListener.SubscriptionScope.DisposeCount);
        Assert.Equal(1, clientScopeFactory.Scopes[1].DisposeCount);
    }

    [Fact]
    public async Task Subscription_Should_ThrowFirstCleanupError_WhenEnumeratorIsDisposed()
    {
        // arrange
        var client = new SingleEventSubscriptionClient();
        var diagnosticListener = new CleanupDiagnosticListener();
        var clientScopeFactory = new TestClientScopeFactory(
            () => client,
            disposeException: new InvalidOperationException("The client scope cleanup failed."));
        var executor = await CreateExecutorAsync(
            new EmptyQueryClient(),
            clientScopeFactory: clientScopeFactory,
            diagnosticListener: diagnosticListener);

        // act
        await using var result = await executor.ExecuteAsync(
            CreateSubscriptionRequest(),
            TestContext.Current.CancellationToken);
        var stream = result.ExpectResponseStream();
        var enumerator = stream.ReadResultsAsync().GetAsyncEnumerator(TestContext.Current.CancellationToken);
        try
        {
            Assert.True(await enumerator.MoveNextAsync());
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await enumerator.DisposeAsync());

            // assert
            Assert.Equal("The event enumerator cleanup failed.", exception.Message);
            Assert.Equal(1, client.DisposeCount);
            Assert.Equal(1, diagnosticListener.SubscriptionScope.DisposeCount);
            Assert.Equal(1, clientScopeFactory.Scopes[1].DisposeCount);
        }
        finally
        {
            await enumerator.DisposeAsync();
        }
    }

    [Fact]
    public async Task Subscription_Should_DisposeDeliveredEventScopeExactlyOnce()
    {
        // arrange
        var diagnosticListener = new SubscriptionNodeDiagnosticListener();
        var executor = await CreateExecutorAsync(
            new YieldThenThrowBeforeMintingSubscriptionClient(),
            diagnosticListener: diagnosticListener);

        // act
        await using var result = await executor.ExecuteAsync(
            CreateSubscriptionRequest(),
            TestContext.Current.CancellationToken);
        var stream = result.ExpectResponseStream();
        var enumerator = stream.ReadResultsAsync().GetAsyncEnumerator(TestContext.Current.CancellationToken);
        try
        {
            Assert.True(await enumerator.MoveNextAsync());

            // assert
            Assert.Equal(1, diagnosticListener.SubscriptionNodeScope.DisposeCount);

            await enumerator.DisposeAsync();
            Assert.Equal(1, diagnosticListener.SubscriptionNodeScope.DisposeCount);
        }
        finally
        {
            await enumerator.DisposeAsync();
        }
    }

    private static async Task<IRequestExecutor> CreateExecutorAsync(
        ISourceSchemaClient client,
        SupportedOperationType supportedOperations = SupportedOperationType.Subscription,
        TestClientScopeFactory? clientScopeFactory = null,
        FusionExecutionDiagnosticEventListener? diagnosticListener = null)
    {
        var services = new ServiceCollection();
        services.AddHttpClient();

        var builder = services
            .AddGraphQLGateway()
            .AddInMemoryConfiguration(
                ComposeSchemaDocument(
                    """
                    # name: events
                    type Query {
                      field: String
                    }

                    type Subscription {
                      onMessage: String
                    }
                    """));

        builder.Services.AddSingleton<ISourceSchemaClientFactory>(
            new TestSubscriptionClientFactory(client));

        if (clientScopeFactory is not null)
        {
            builder.Services.AddSingleton<ISourceSchemaClientScopeFactory>(clientScopeFactory);
        }

        if (diagnosticListener is not null)
        {
            builder.AddDiagnosticEventListener(_ => diagnosticListener);
        }

        FusionSetupUtilities.Configure(
            builder,
            setup => setup.ClientConfigurationModifiers.Add(
                _ => new TestSubscriptionClientConfiguration("events", supportedOperations)));

        return await services.BuildGatewayAsync(TestContext.Current.CancellationToken);
    }

    private static IOperationRequest CreateQueryRequest()
        => OperationRequestBuilder.New()
            .SetDocument(
                """
                query {
                  field
                }
                """)
            .Build();

    private static IOperationRequest CreateSubscriptionRequest()
        => OperationRequestBuilder.New()
            .SetDocument(
                """
                subscription {
                  onMessage
                }
                """)
            .Build();

    private abstract class TestSubscriptionClient : ISourceSchemaClient
    {
        public SourceSchemaClientCapabilities Capabilities => SourceSchemaClientCapabilities.None;

        public virtual IAsyncEnumerable<SourceSchemaResult> ExecuteAsync(
            OperationPlanContext context,
            SourceSchemaClientRequest request,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public IAsyncEnumerable<SourceSchemaBatchResult> ExecuteBatchAsync(
            OperationPlanContext context,
            ImmutableArray<SourceSchemaClientRequest> requests,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public abstract IAsyncEnumerable<SourceSchemaResult> SubscribeAsync(
            OperationPlanContext context,
            SourceSchemaClientRequest request,
            CancellationToken cancellationToken);

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class EmptyQueryClient : TestSubscriptionClient
    {
        public override async IAsyncEnumerable<SourceSchemaResult> ExecuteAsync(
            OperationPlanContext context,
            SourceSchemaClientRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.Yield();
            yield break;
        }

        public override IAsyncEnumerable<SourceSchemaResult> SubscribeAsync(
            OperationPlanContext context,
            SourceSchemaClientRequest request,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();
    }

    private sealed class ThrowingSubscriptionClient : TestSubscriptionClient
    {
        private readonly List<IMemoryArena> _mintedArenas = [];

        public IReadOnlyList<IMemoryArena> MintedArenas => _mintedArenas;

        public override async IAsyncEnumerable<SourceSchemaResult> SubscribeAsync(
            OperationPlanContext context,
            SourceSchemaClientRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                yield break;
            }

            var arena = context.MemorySource.GetNextArena();
            _mintedArenas.Add(arena);
            arena.Rent(1);

            await Task.Yield();
            throw new InvalidOperationException("The subscription event failed after minting an arena.");
        }
    }

    private sealed class YieldThenThrowBeforeMintingSubscriptionClient : TestSubscriptionClient
    {
        private static readonly byte[] s_payload = """{"data":{"onMessage":"first"}}"""u8.ToArray();
        private readonly List<IMemoryArena> _mintedArenas = [];

        public IReadOnlyList<IMemoryArena> MintedArenas => _mintedArenas;

        public override async IAsyncEnumerable<SourceSchemaResult> SubscribeAsync(
            OperationPlanContext context,
            SourceSchemaClientRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var arena = context.MemorySource.GetNextArena();
            _mintedArenas.Add(arena);
            var document = SourceResultDocument.Parse(arena, s_payload, s_payload.Length);

            yield return new SourceSchemaResult(CompactPath.Root, document);

            await Task.Yield();
            throw new InvalidOperationException("The next subscription event failed before minting an arena.");
        }
    }

    private sealed class ScopeBorrowSubscriptionClient : TestSubscriptionClient
    {
        public ISourceSchemaClient? DependentClient { get; private set; }

        public override async IAsyncEnumerable<SourceSchemaResult> SubscribeAsync(
            OperationPlanContext context,
            SourceSchemaClientRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            DependentClient = context.GetClient("events", OperationType.Query);
            await Task.Yield();
            yield break;
        }
    }

    private sealed class ThrowingScopeBorrowSubscriptionClient : TestSubscriptionClient
    {
        public override async IAsyncEnumerable<SourceSchemaResult> SubscribeAsync(
            OperationPlanContext context,
            SourceSchemaClientRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.Yield();
            await Task.FromException(new InvalidOperationException("The subscription failed."));
            yield break;
        }
    }

    private sealed class ThrowingMoveNextSubscriptionClient(Exception exception) : TestSubscriptionClient
    {
        public int DisposeCount { get; private set; }

        public override IAsyncEnumerable<SourceSchemaResult> SubscribeAsync(
            OperationPlanContext context,
            SourceSchemaClientRequest request,
            CancellationToken cancellationToken)
            => new ThrowingAsyncEnumerable(exception, this);

        private sealed class ThrowingAsyncEnumerable(
            Exception exception,
            ThrowingMoveNextSubscriptionClient owner) : IAsyncEnumerable<SourceSchemaResult>
        {
            public IAsyncEnumerator<SourceSchemaResult> GetAsyncEnumerator(
                CancellationToken cancellationToken = default)
                => new ThrowingAsyncEnumerator(exception, owner);
        }

        private sealed class ThrowingAsyncEnumerator(
            Exception exception,
            ThrowingMoveNextSubscriptionClient owner) : IAsyncEnumerator<SourceSchemaResult>
        {
            public SourceSchemaResult Current => null!;

            public ValueTask<bool> MoveNextAsync() => ValueTask.FromException<bool>(exception);

            public ValueTask DisposeAsync()
            {
                owner.DisposeCount++;
                return ValueTask.FromException(
                    new InvalidOperationException("The event enumerator cleanup failed."));
            }
        }
    }

    private sealed class CancelledSubscriptionClient(CancellationTokenSource cancellationTokenSource)
        : TestSubscriptionClient
    {
        public int DisposeCount { get; private set; }

        public override IAsyncEnumerable<SourceSchemaResult> SubscribeAsync(
            OperationPlanContext context,
            SourceSchemaClientRequest request,
            CancellationToken cancellationToken)
            => new CancelledAsyncEnumerable(this, cancellationTokenSource);

        private sealed class CancelledAsyncEnumerable(
            CancelledSubscriptionClient owner,
            CancellationTokenSource cancellationTokenSource) : IAsyncEnumerable<SourceSchemaResult>
        {
            public IAsyncEnumerator<SourceSchemaResult> GetAsyncEnumerator(
                CancellationToken cancellationToken = default)
                => new CancelledAsyncEnumerator(owner, cancellationTokenSource);
        }

        private sealed class CancelledAsyncEnumerator(
            CancelledSubscriptionClient owner,
            CancellationTokenSource cancellationTokenSource) : IAsyncEnumerator<SourceSchemaResult>
        {
            public SourceSchemaResult Current => null!;

            public ValueTask<bool> MoveNextAsync()
            {
                cancellationTokenSource.Cancel();
                return ValueTask.FromException<bool>(
                    new OperationCanceledException(cancellationTokenSource.Token));
            }

            public ValueTask DisposeAsync()
            {
                owner.DisposeCount++;
                return ValueTask.FromException(
                    new InvalidOperationException("The event enumerator cleanup failed."));
            }
        }
    }

    private sealed class CompletingSubscriptionClient : TestSubscriptionClient
    {
        public int DisposeCount { get; private set; }

        public override IAsyncEnumerable<SourceSchemaResult> SubscribeAsync(
            OperationPlanContext context,
            SourceSchemaClientRequest request,
            CancellationToken cancellationToken)
            => new CompletingAsyncEnumerable(this);

        private sealed class CompletingAsyncEnumerable(CompletingSubscriptionClient owner)
            : IAsyncEnumerable<SourceSchemaResult>
        {
            public IAsyncEnumerator<SourceSchemaResult> GetAsyncEnumerator(
                CancellationToken cancellationToken = default)
                => new CompletingAsyncEnumerator(owner);
        }

        private sealed class CompletingAsyncEnumerator(CompletingSubscriptionClient owner)
            : IAsyncEnumerator<SourceSchemaResult>
        {
            public SourceSchemaResult Current => null!;

            public ValueTask<bool> MoveNextAsync() => ValueTask.FromResult(false);

            public ValueTask DisposeAsync()
            {
                owner.DisposeCount++;
                return ValueTask.FromException(
                    new InvalidOperationException("The event enumerator cleanup failed."));
            }
        }
    }

    private sealed class SingleEventSubscriptionClient : TestSubscriptionClient
    {
        private static readonly byte[] s_payload = """{"data":{"onMessage":"first"}}"""u8.ToArray();

        public int DisposeCount { get; private set; }

        public override async IAsyncEnumerable<SourceSchemaResult> SubscribeAsync(
            OperationPlanContext context,
            SourceSchemaClientRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            try
            {
                var arena = context.MemorySource.GetNextArena();
                var document = SourceResultDocument.Parse(arena, s_payload, s_payload.Length);
                yield return new SourceSchemaResult(CompactPath.Root, document);

                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
            finally
            {
                DisposeCount++;
                throw new InvalidOperationException("The event enumerator cleanup failed.");
            }
        }
    }

    private sealed class TestSubscriptionClientFactory(ISourceSchemaClient client)
        : ISourceSchemaClientFactory
    {
        public bool CanHandle(ISourceSchemaClientConfiguration configuration)
            => configuration is TestSubscriptionClientConfiguration;

        public ISourceSchemaClient CreateClient(
            FusionSchemaDefinition schema,
            ISourceSchemaClientConfiguration configuration)
            => client;
    }

    private sealed class TestClientScopeFactory : ISourceSchemaClientScopeFactory
    {
        private readonly Func<ISourceSchemaClient> _createClient;
        private readonly Exception? _getClientException;
        private readonly Exception? _disposeException;

        public TestClientScopeFactory(
            Func<ISourceSchemaClient>? createClient = null,
            Exception? getClientException = null,
            Exception? disposeException = null)
        {
            _createClient = createClient ?? (() => new ScopeBorrowSubscriptionClient());
            _getClientException = getClientException;
            _disposeException = disposeException;
        }

        public List<TestClientScope> Scopes { get; } = [];

        public ISourceSchemaClientScope CreateScope(ISchemaDefinition schemaDefinition)
        {
            var scope = new TestClientScope(
                _createClient(),
                _getClientException,
                Scopes.Count == 0 ? null : _disposeException);
            Scopes.Add(scope);
            return scope;
        }
    }

    private sealed class TestClientScope(
        ISourceSchemaClient client,
        Exception? getClientException = null,
        Exception? disposeException = null) : ISourceSchemaClientScope
    {
        public ISourceSchemaClient Client { get; } = client;

        public int DisposeCount { get; private set; }

        public ISourceSchemaClient GetClient(string schemaName, OperationType operationType)
            => getClientException is null ? Client : throw getClientException;

        public ValueTask DisposeAsync()
        {
            DisposeCount++;
            return disposeException is null
                ? ValueTask.CompletedTask
                : ValueTask.FromException(disposeException);
        }
    }

    private sealed class CleanupDiagnosticListener : FusionExecutionDiagnosticEventListener
    {
        public TrackingScope SubscriptionScope { get; } =
            new(new InvalidOperationException("The subscription scope cleanup failed."));

        public override IDisposable ExecuteSubscription(RequestContext context, ulong subscriptionId)
            => SubscriptionScope;
    }

    private sealed class SubscriptionNodeDiagnosticListener : FusionExecutionDiagnosticEventListener
    {
        public TrackingScope SubscriptionNodeScope { get; } = new();

        public override IDisposable ExecuteSubscriptionNode(
            OperationPlanContext context,
            ExecutionNode node,
            string schemaName,
            ulong subscriptionId)
            => SubscriptionNodeScope;
    }

    private sealed class TrackingScope(Exception? exception = null) : IDisposable
    {
        public int DisposeCount { get; private set; }

        public void Dispose()
        {
            DisposeCount++;

            if (exception is not null)
            {
                throw exception;
            }
        }
    }

    private sealed class TestSubscriptionClientConfiguration(
        string name,
        SupportedOperationType supportedOperations)
        : ISourceSchemaClientConfiguration
    {
        public string Name { get; } = name;

        public SupportedOperationType SupportedOperations { get; } = supportedOperations;
    }
}
