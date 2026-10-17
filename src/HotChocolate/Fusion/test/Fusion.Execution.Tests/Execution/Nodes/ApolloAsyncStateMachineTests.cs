using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Text.Json;
using System.Runtime.CompilerServices;
using System.Text;
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

public sealed class ApolloAsyncStateMachineTests : FusionTestBase
{
    [Theory]
    [InlineData(false, "success")]
    [InlineData(false, "transport")]
    [InlineData(false, "missing")]
    [InlineData(false, "null")]
    [InlineData(false, "protocol")]
    [InlineData(false, "source-error")]
    [InlineData(false, "merge-error")]
    [InlineData(false, "cancel")]
    [InlineData(true, "success")]
    [InlineData(true, "transport")]
    [InlineData(true, "missing")]
    [InlineData(true, "fallback")]
    [InlineData(true, "null")]
    [InlineData(true, "protocol")]
    [InlineData(true, "source-error")]
    [InlineData(true, "merge-error")]
    [InlineData(true, "cancel")]
    public async Task ExecuteAsync_Should_PreserveResultAndErrorPaths_When_SourceSuspends(
        bool batch,
        string response)
    {
        // arrange
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(
            TestContext.Current.CancellationToken);
        var client = new TestClient(response, cancellation);
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
        await using var provider = services.BuildServiceProvider();
        var executor = await provider.GetRequiredService<IRequestExecutorProvider>()
            .GetExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);
        var request = OperationRequestBuilder.New()
            .SetDocument(batch
                ? "{ products { name } categories { title } }"
                : "{ products { name } }")
            .Build();

        // act
        await using var result = await executor.ExecuteAsync(request, cancellation.Token);

        // assert
        Assert.Equal(
            batch
                ? ["ApolloOperationBatchExecutionNode: 2"]
                : new[] { "ApolloOperationExecutionNode" },
            client.Dispatches);
        var snapshot = Snapshot.Create($"{batch}-{response}")
            .Add(result.ToJson(), "Result", MarkdownLanguages.Json)
            .Add(client.Dispatches, "Apollo Dispatches");

        if (response is "cancel")
        {
            snapshot.Add(
                client.Documents.Select(document =>
                    Record.Exception(() => document.Root.GetProperty("data"))?.GetType().Name),
                "Canceled Result Disposal");
        }

        snapshot.MatchMarkdownSnapshot();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteAsync_Should_PreservePerRequestContext_When_SuspendedRequestsOverlap(bool batch)
    {
        // arrange
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(
            TestContext.Current.CancellationToken);
        var client = new TestClient("success", cancellation);
        var services = new ServiceCollection();
        services.AddHttpClient();
        var builder = services.AddGraphQLGateway().AddInMemoryConfiguration(ComposeApolloSchema());
        builder.Services.AddSingleton<ISourceSchemaClientFactory>(new TestClientFactory(client));
        FusionSetupUtilities.Configure(builder, setup =>
        {
            setup.ClientConfigurationModifiers.Add(_ => new TestClientConfiguration("a"));
            setup.ClientConfigurationModifiers.Add(_ => new TestClientConfiguration("b"));
        });
        await using var provider = services.BuildServiceProvider();
        var executor = await provider.GetRequiredService<IRequestExecutorProvider>()
            .GetExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);
        var request = OperationRequestBuilder.New()
            .SetDocument(batch
                ? "{ products { name } categories { title } }"
                : "{ products { name } }")
            .Build();

        // act
        var observed = new List<string>();
        for (var wave = 0; wave < 3; wave++)
        {
            var tasks = Enumerable.Range(wave * 16, 16).Select(async index =>
            {
                client.Correlation.Value = index.ToString();
                await using var result = await executor.ExecuteAsync(request, cancellation.Token);
                using var document = JsonDocument.Parse(result.ToJson());
                var data = document.RootElement.GetProperty("data");
                var names = data.GetProperty("products").EnumerateArray()
                    .Select(value => value.GetProperty("name").GetString());
                var values = string.Join(",", names);
                if (batch)
                {
                    values += ";" + string.Join(",", data.GetProperty("categories").EnumerateArray()
                        .Select(value => value.GetProperty("title").GetString()));
                }

                return values;
            });
            observed.AddRange(await Task.WhenAll(tasks));
        }

        // assert
        var expected = Enumerable.Range(0, 48).Select(index =>
        {
            var values = $"One-{index},One-{index},Two-{index}";
            return batch ? values + ";" + values : values;
        });
        Assert.Equal(expected, observed);
        Assert.Equal(
            Enumerable.Repeat(batch ? "ApolloOperationBatchExecutionNode: 2" : "ApolloOperationExecutionNode", 48),
            client.Dispatches);
    }

    private static DocumentNode ComposeApolloSchema()
    {
        const string sourceSchemaA =
            """
            schema @link(url: "https://specs.apollo.dev/federation/v2.6", import: ["@key", "@listSize"]) {
              query: Query
            }
            type Product @key(fields: "id") { id: ID! }
            type Category @key(fields: "id") { id: ID! }
            type Query {
              products: [Product] @listSize(assumedSize: 3)
              categories: [Category] @listSize(assumedSize: 3)
              _service: _Service!
              _entities(representations: [_Any!]!): [_Entity]!
            }
            type _Service { sdl: String! }
            union _Entity = Product | Category
            scalar FieldSet
            scalar _Any
            directive @key(fields: FieldSet! resolvable: Boolean = true) repeatable on OBJECT | INTERFACE
            directive @link(url: String! import: [String!]) repeatable on SCHEMA
            directive @listSize(assumedSize: Int slicingArguments: [String!] sizedFields: [String!] requireOneSlicingArgument: Boolean = true) on FIELD_DEFINITION
            """;
        const string sourceSchemaB =
            """
            schema @link(url: "https://specs.apollo.dev/federation/v2.6", import: ["@key", "@listSize"]) {
              query: Query
            }
            type Product @key(fields: "id") { id: ID! name: String }
            type Category @key(fields: "id") { id: ID! title: String }
            type Query {
              _service: _Service!
              _entities(representations: [_Any!]!): [_Entity]!
            }
            type _Service { sdl: String! }
            union _Entity = Product | Category
            scalar FieldSet
            scalar _Any
            directive @key(fields: FieldSet! resolvable: Boolean = true) repeatable on OBJECT | INTERFACE
            directive @link(url: String! import: [String!]) repeatable on SCHEMA
            directive @listSize(assumedSize: Int slicingArguments: [String!] sizedFields: [String!] requireOneSlicingArgument: Boolean = true) on FIELD_DEFINITION
            """;
        var sources = new[]
        {
            new SourceSchemaText("a", sourceSchemaA),
            new SourceSchemaText("b", sourceSchemaB)
        };
        var options = new SchemaComposerOptions();

        foreach (var source in sources)
        {
            options.SourceSchemas[source.Name] = new SourceSchemaOptions
            {
                Preprocessor = new SourceSchemaPreprocessorOptions { InferKeysFromLookups = false }
            };
        }

        return new SchemaComposer(sources, options, new CompositionLog()).Compose().Value.ToSyntaxNode();
    }

    private sealed class TestClient(string response, CancellationTokenSource cancellation) : ISourceSchemaClient
    {
        private static readonly byte[] s_root =
            """{"data":{"products":[{"__typename":"Product","id":"1"},{"__typename":"Product","id":"1"},{"__typename":"Product","id":"2"}],"categories":[{"__typename":"Category","id":"1"},{"__typename":"Category","id":"1"},{"__typename":"Category","id":"2"}]}}"""u8.ToArray();

        public SourceSchemaClientCapabilities Capabilities => SourceSchemaClientCapabilities.None;

        public ConcurrentQueue<string> Dispatches { get; } = new();

        public AsyncLocal<string?> Correlation { get; } = new();

        public ConcurrentQueue<SourceResultDocument> Documents { get; } = new();

        public async IAsyncEnumerable<SourceSchemaResult> ExecuteAsync(
            OperationPlanContext context,
            SourceSchemaClientRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            if (request.Node is not ApolloOperationExecutionNode)
            {
                yield return new SourceSchemaResult(
                    CompactPath.Root,
                    SourceResultDocument.Parse(context.Memory, s_root, s_root.Length));
                yield break;
            }

            await Task.Yield();
            Dispatches.Enqueue(request.Node.GetType().Name);

            if (response is "transport")
            {
                throw new InvalidOperationException("Source transport failed.");
            }

            if (response is "missing")
            {
                yield break;
            }

            var result = CreateResult(context);
            yield return result;

            if (response is "cancel")
            {
                cancellation.Cancel();
                cancellationToken.ThrowIfCancellationRequested();
            }

            if (response is "merge-error")
            {
                result.Dispose();
            }

            await Task.CompletedTask;
        }

        public async IAsyncEnumerable<SourceSchemaBatchResult> ExecuteBatchAsync(
            OperationPlanContext context,
            ImmutableArray<SourceSchemaClientRequest> requests,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.Yield();
            Dispatches.Enqueue($"{requests[0].Node.GetType().Name}: {requests.Length}");

            if (response is "transport")
            {
                throw new InvalidOperationException("Source transport failed.");
            }

            for (var i = 0; i < requests.Length; i++)
            {
                if (i == 0 && response is "missing" or "fallback")
                {
                    if (response is "fallback")
                    {
                        context.TrackBatchRequestError(
                            requests[i].Node,
                            i,
                            new InvalidOperationException("Source fallback failed."));
                    }

                    continue;
                }

                var result = CreateResult(context);
                yield return new SourceSchemaBatchResult(i, result);

                if (response is "cancel")
                {
                    cancellation.Cancel();
                    cancellationToken.ThrowIfCancellationRequested();
                }

                if (response is "merge-error")
                {
                    result.Dispose();
                }
            }

            await Task.CompletedTask;
        }

        private SourceSchemaResult CreateResult(OperationPlanContext context)
        {
            var json = response switch
            {
                "null" => """{"data":{"_entities":[null,null]}}""",
                "protocol" => """{"data":{"_entities":{}}}""",
                "source-error" =>
                    """{"data":{"_entities":[null,{"name":"Two","title":"Two"}]},"errors":[{"message":"Entity failed.","path":["_entities",0]}]}""",
                _ =>
                    """{"data":{"_entities":[{"name":"One","title":"One"},{"name":"Two","title":"Two"}]}}"""
            };
            if (Correlation.Value is { } correlation)
            {
                json = json.Replace("One", "One-" + correlation, StringComparison.Ordinal)
                    .Replace("Two", "Two-" + correlation, StringComparison.Ordinal);
            }

            var bytes = Encoding.UTF8.GetBytes(json);
            var document = SourceResultDocument.Parse(context.Memory, bytes, bytes.Length);
            Documents.Enqueue(document);
            return new SourceSchemaResult(CompactPath.Root, document);
        }

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
