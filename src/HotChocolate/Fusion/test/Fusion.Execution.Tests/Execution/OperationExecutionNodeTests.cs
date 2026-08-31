using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Fusion.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution;

public sealed class OperationExecutionNodeTests : FusionTestBase
{
    [Fact]
    public async Task ExecuteAsync_Should_StreamDeferredResults_When_GatewayPlanHasNoPolicies()
    {
        // arrange
        var executor = await CreateDeferExecutorAsync(new DeferredQueryClient());

        // act
        await using var result = await executor.ExecuteAsync(
            """
            query {
              immediate
              ... @defer {
                deferred
              }
            }
            """,
            TestContext.Current.CancellationToken);
        await using var stream = result.ExpectResponseStream();
        var responses = new List<string>();

        await foreach (var response in stream
            .ReadResultsAsync()
            .WithCancellation(TestContext.Current.CancellationToken))
        {
            responses.Add(response.ToJson());
        }

        // assert
        responses.MatchInlineSnapshots(
        [
            """
            {
              "data": {
                "immediate": "initial"
              },
              "pending": [
                {
                  "id": "0",
                  "path": []
                }
              ],
              "hasNext": true
            }
            """,
            """
            {
              "incremental": [
                {
                  "id": "0",
                  "data": {
                    "deferred": "incremental"
                  }
                }
              ],
              "completed": [
                {
                  "id": "0"
                }
              ],
              "hasNext": false
            }
            """
        ]);
    }

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
        var mintedArena = client.MintedArenas.Single();

        // assert
        var arena = Assert.IsType<MemoryArena>(mintedArena);
        Assert.False(hasResult);
        Assert.Throws<ObjectDisposedException>(() => arena.Rent(1));
        Assert.Equal(0, arena.RentedPageCount);
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
        var firstResult = enumerator.Current;
        var mintedArena = client.MintedArenas.Single();
        var hasSecondResult = await enumerator.MoveNextAsync();

        // assert
        try
        {
            var firstArena = Assert.IsType<MemoryArena>(mintedArena);
            Assert.True(hasFirstResult);
            Assert.False(hasSecondResult);
            Assert.False(firstArena.IsDisposed);
        }
        finally
        {
            await firstResult.DisposeAsync();
        }
    }

    private static async Task<IRequestExecutor> CreateExecutorAsync(
        ISourceSchemaClient client,
        SupportedOperationType supportedOperations = SupportedOperationType.Subscription)
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

        FusionSetupUtilities.Configure(
            builder,
            setup => setup.ClientConfigurationModifiers.Add(
                _ => new TestSubscriptionClientConfiguration("events", supportedOperations)));

        return await services.BuildGatewayAsync(TestContext.Current.CancellationToken);
    }

    private static async Task<IRequestExecutor> CreateDeferExecutorAsync(ISourceSchemaClient client)
    {
        var services = new ServiceCollection();
        services.AddHttpClient();

        var builder = services
            .AddGraphQLGateway()
            .ModifyOptions(o => o.EnableDefer = true)
            .AddInMemoryConfiguration(
                ComposeSchemaDocument(
                    """
                    # name: source
                    type Query {
                      immediate: String
                      deferred: String
                    }
                    """));

        builder.Services.AddSingleton<ISourceSchemaClientFactory>(
            new TestSubscriptionClientFactory(client));

        FusionSetupUtilities.Configure(
            builder,
            setup => setup.ClientConfigurationModifiers.Add(
                _ => new TestSubscriptionClientConfiguration("source", SupportedOperationType.Query)));

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

    private sealed class DeferredQueryClient : TestSubscriptionClient
    {
        private static readonly byte[] s_initialPayload = """{"data":{"immediate":"initial"}}"""u8.ToArray();
        private static readonly byte[] s_incrementalPayload =
            """{"data":{"deferred":"incremental"}}"""u8.ToArray();

        public override async IAsyncEnumerable<SourceSchemaResult> ExecuteAsync(
            OperationPlanContext context,
            SourceSchemaClientRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var payload = request.OperationSourceText.Contains("immediate", StringComparison.Ordinal)
                ? s_initialPayload
                : s_incrementalPayload;
            var arena = context.MemorySource.GetNextArena();
            var document = SourceResultDocument.Parse(arena, payload, payload.Length);
            await Task.Yield();
            yield return new SourceSchemaResult(CompactPath.Root, document);
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

    private sealed class TestSubscriptionClientConfiguration(
        string name,
        SupportedOperationType supportedOperations)
        : ISourceSchemaClientConfiguration
    {
        public string Name { get; } = name;

        public SupportedOperationType SupportedOperations { get; } = supportedOperations;
    }
}
