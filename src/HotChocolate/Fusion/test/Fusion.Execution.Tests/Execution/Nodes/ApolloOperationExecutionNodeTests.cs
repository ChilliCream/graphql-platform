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
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task ExecuteAsync_Should_DisposeUnmergedResult_When_SourceStreamFails(
        bool duplicateEntities,
        bool throwException)
    {
        // arrange
        using var arena = new MemoryArena();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(
            TestContext.Current.CancellationToken);
        var client = new YieldThenFailClient(arena, cancellation, duplicateEntities, throwException);
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
        var request = OperationRequestBuilder.New()
            .SetDocument(duplicateEntities ? "{ products { name } }" : "{ product { name } }")
            .Build();

        // act
        await using var result = await executor.ExecuteAsync(request, cancellation.Token);

        // assert
        Assert.True(client.ApolloDispatched, result.ToJson());
        Assert.Equal(
            nameof(ObjectDisposedException),
            Record.Exception(() => client.Document!.Root.GetProperty("data"))?.GetType().Name);
        Assert.True(client.EnumeratorDisposed);
        if (throwException)
        {
            Assert.Equal(
                [Path.Root.Append("products").Append(0).Append("name"),
                    Path.Root.Append("products").Append(1).Append("name")],
                result.ExpectOperationResult().Errors!.Select(error => error.Path));
        }
        else
        {
            Assert.Equal(
                ErrorCodes.Execution.Canceled,
                result.ExpectOperationResult().Errors!.Single().Extensions!["code"]);
        }
    }

    private static DocumentNode ComposeApolloSchema()
    {
        const string sourceSchemaA =
            """
            schema @link(url: "https://specs.apollo.dev/federation/v2.6", import: ["@key"]) {
              query: Query
            }
            directive @listSize(assumedSize: Int, slicingArguments: [String!], sizedFields: [String!], requireOneSlicingArgument: Boolean = true) on FIELD_DEFINITION
            type Product @key(fields: "id") { id: ID! }
            type Query {
              product: Product
              products: [Product] @listSize(assumedSize: 2, requireOneSlicingArgument: false)
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

    private sealed class YieldThenFailClient(
        MemoryArena arena,
        CancellationTokenSource cancellation,
        bool duplicateEntities,
        bool throwException) : ISourceSchemaClient
    {
        private static readonly byte[] s_root =
            """{"data":{"product":{"__typename":"Product","id":"1"}}}"""u8.ToArray();
        private static readonly byte[] s_duplicates =
            """{"data":{"products":[{"__typename":"Product","id":"1"},{"__typename":"Product","id":"1"}]}}"""u8.ToArray();
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
                var payload = duplicateEntities ? s_duplicates : s_root;
                var root = SourceResultDocument.Parse(context.Memory, payload, payload.Length);
                yield return new SourceSchemaResult(CompactPath.Root, root);
                yield break;
            }

            ApolloDispatched = true;

            try
            {
                Document = SourceResultDocument.Parse(arena, s_entity, s_entity.Length);
                yield return new SourceSchemaResult(CompactPath.Root, Document);
                await Task.Yield();
                if (throwException)
                {
                    throw new InvalidOperationException("Transport failed.");
                }

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
