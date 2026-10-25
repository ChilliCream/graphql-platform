using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution.Nodes;

public sealed class ApolloOperationExecutionNodeTests : FusionTestBase
{
    [Fact]
    public async Task ExecuteAsync_Should_DisposeUnmergedResult_When_SourceStreamIsCanceled()
    {
        // arrange
        using var arena = new MemoryArena();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(
            TestContext.Current.CancellationToken);
        var client = new YieldThenCancelClient(arena, cancellation);
        var services = new ServiceCollection();
        services.AddHttpClient();
        var builder = services.AddGraphQLGateway().AddInMemoryConfiguration(ComposeApolloSchema());
        builder.Services.AddSingleton<ISourceSchemaClientFactory>(new TestClientFactory(client));
        FusionSetupUtilities.Configure(
            builder,
            setup =>
            {
                setup.ClientConfigurationModifiers.Add(_ => new TestClientConfiguration("a"));
                setup.ClientConfigurationModifiers.Add(_ => new TestClientConfiguration("b"));
            });
        var executor = await services.BuildGatewayAsync(TestContext.Current.CancellationToken);
        var request = OperationRequestBuilder.New().SetDocument("{ product { name } }").Build();

        // act
        await using var result = await executor.ExecuteAsync(request, cancellation.Token);

        // assert
        Assert.True(client.ApolloDispatched, result.ToJson());
        Assert.Equal(
            nameof(ObjectDisposedException),
            Record.Exception(() => client.Document!.Root.GetProperty("data"))?.GetType().Name);
        Assert.True(client.EnumeratorDisposed);
        Assert.Equal(
            ErrorCodes.Execution.Canceled,
            result.ExpectOperationResult().Errors!.Single().Extensions!["code"]);
    }

    private static DocumentNode ComposeApolloSchema()
    {
        const string sourceSchemaA =
            """
            schema @link(url: "https://specs.apollo.dev/federation/v2.6", import: ["@key"]) {
              query: Query
            }
            type Product @key(fields: "id") { id: ID! }
            type Query {
              product: Product
              _service: _Service!
              _entities(representations: [_Any!]!): [_Entity]!
            }
            type _Service { sdl: String! }
            union _Entity = Product
            scalar FieldSet
            scalar _Any
            directive @key(fields: FieldSet! resolvable: Boolean = true) repeatable on OBJECT | INTERFACE
            directive @link(url: String! import: [String!]) repeatable on SCHEMA
            """;
        const string sourceSchemaB =
            """
            schema @link(url: "https://specs.apollo.dev/federation/v2.6", import: ["@key"]) {
              query: Query
            }
            type Product @key(fields: "id") { id: ID! name: String }
            type Query {
              _service: _Service!
              _entities(representations: [_Any!]!): [_Entity]!
            }
            type _Service { sdl: String! }
            union _Entity = Product
            scalar FieldSet
            scalar _Any
            directive @key(fields: FieldSet! resolvable: Boolean = true) repeatable on OBJECT | INTERFACE
            directive @link(url: String! import: [String!]) repeatable on SCHEMA
            """;
        var sourceTexts = new[]
        {
            new SourceSchemaText("a", sourceSchemaA),
            new SourceSchemaText("b", sourceSchemaB)
        };
        var options = new SchemaComposerOptions();

        foreach (var source in sourceTexts)
        {
            options.SourceSchemas[source.Name] = new SourceSchemaOptions
            {
                Preprocessor = new SourceSchemaPreprocessorOptions { InferKeysFromLookups = false }
            };
        }

        var result = new SchemaComposer(sourceTexts, options, new CompositionLog()).Compose();
        return result.Value.ToSyntaxNode();
    }

    private sealed class YieldThenCancelClient(
        MemoryArena arena,
        CancellationTokenSource cancellation) : ISourceSchemaClient
    {
        private static readonly byte[] s_root =
            """{"data":{"product":{"__typename":"Product","id":"1"}}}"""u8.ToArray();
        private static readonly byte[] s_entity =
            """{"data":{"_entities":[{"name":"value"}]}}"""u8.ToArray();

        public SourceSchemaClientCapabilities Capabilities => SourceSchemaClientCapabilities.None;

        public SourceResultDocument? Document { get; private set; }

        public bool ApolloDispatched { get; private set; }

        public bool EnumeratorDisposed { get; private set; }

        public async IAsyncEnumerable<SourceSchemaResult> ExecuteAsync(
            OperationPlanContext context,
            SourceSchemaClientRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            if (request.Node is not ApolloOperationExecutionNode)
            {
                var root = SourceResultDocument.Parse(context.Memory, s_root, s_root.Length);
                yield return new SourceSchemaResult(CompactPath.Root, root);
                yield break;
            }

            ApolloDispatched = true;

            try
            {
                Document = SourceResultDocument.Parse(arena, s_entity, s_entity.Length);
                yield return new SourceSchemaResult(CompactPath.Root, Document);
                await Task.Yield();
                cancellation.Cancel();
                cancellationToken.ThrowIfCancellationRequested();
            }
            finally
            {
                EnumeratorDisposed = true;
            }
        }

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

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class TestClientFactory(ISourceSchemaClient client) : ISourceSchemaClientFactory
    {
        public bool CanHandle(ISourceSchemaClientConfiguration configuration)
            => configuration is TestClientConfiguration;

        public ISourceSchemaClient CreateClient(
            FusionSchemaDefinition schema,
            ISourceSchemaClientConfiguration configuration)
            => client;
    }

    private sealed class TestClientConfiguration(string name) : ISourceSchemaClientConfiguration
    {
        public string Name { get; } = name;

        public SupportedOperationType SupportedOperations => SupportedOperationType.Query;
    }
}
