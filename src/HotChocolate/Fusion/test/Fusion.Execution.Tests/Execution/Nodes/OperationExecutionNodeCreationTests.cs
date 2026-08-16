using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using HotChocolate.Execution;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution;

public sealed class OperationExecutionNodeCreationTests : FusionTestBase
{
    private const string SourceSchemaA =
        """
        type Query {
          books: [Book!]!
          bookById(id: ID!): Book @lookup @internal
        }

        type Book @key(fields: "id") {
          id: ID!
          title: String!
        }
        """;

    private const string SourceSchemaB =
        """
        type Query {
          bookById(id: ID!): Book @lookup @internal
        }

        type Book @key(fields: "id") {
          id: ID!
          rating: String!
        }
        """;

    [Fact]
    public void Plan_Should_CarryTheParsedDocument_When_NodeIsCreated()
    {
        // arrange
        var schema = ComposeSchema(SourceSchemaA, SourceSchemaB);

        // act
        var plan = PlanOperation(schema, "{ books { rating } }");

        // assert
        // The root node and the lookup node both carry the syntax tree of their operation.
        Assert.All(
            plan.AllNodes.OfType<OperationExecutionNode>(),
            node => Assert.NotNull(node.OperationDocument));
    }

    [Fact]
    public async Task Client_Should_ReceiveTheNodeDocument_When_TheLookupIsExecuted()
    {
        // arrange
        var client = new RecordingClient();
        var executor = await CreateExecutorAsync(client);

        // act
        await using var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New().SetDocument("{ books { rating } }").Build(),
            TestContext.Current.CancellationToken);

        // assert
        var request = Assert.Single(client.Requests, r => r.LookupTypeName is not null);
        var node = Assert.IsType<OperationExecutionNode>(request.Node);
        Assert.NotNull(request.OperationDocument);
        Assert.Same(node.OperationDocument, request.OperationDocument);
    }

    [Fact]
    public void Plan_Should_CarryTheLookupTypeName_When_NodeReadsFromALookupField()
    {
        // arrange
        var schema = ComposeSchema(SourceSchemaA, SourceSchemaB);

        // act
        var plan = PlanOperation(schema, "{ books { rating } }");

        // assert
        Assert.Equal("Book", GetLookupNode(plan).LookupTypeName);
    }

    [Fact]
    public void Plan_Should_CarryNoLookupTypeName_When_NodeReadsFromTheRoot()
    {
        // arrange
        var schema = ComposeSchema(SourceSchemaA, SourceSchemaB);

        // act
        var plan = PlanOperation(schema, "{ books { title } }");

        // assert
        var root = Assert.Single(plan.AllNodes.OfType<OperationExecutionNode>());
        Assert.Null(root.LookupTypeName);
    }

    [Fact]
    public void Create_Should_Throw_When_SourceTextDoesNotParse()
    {
        // arrange
        var schema = ComposeSchema(SourceSchemaA, SourceSchemaB);
        var plan = PlanOperation(schema, "{ books { title } }");
        var node = Assert.Single(plan.AllNodes.OfType<OperationExecutionNode>());
        var sourceText = "query { books "u8.ToArray();
        var operation = new OperationSourceText(
            "Op_1",
            OperationType.Query,
            sourceText,
            OperationSourceTextHash.Compute(sourceText));

        // act
        void Act() => new OperationExecutionNode(
            node.Id,
            operation,
            lookupTypeName: null,
            node.SchemaName,
            node.Target,
            node.Source,
            [],
            [],
            node.ResultSelectionSet,
            [],
            requiresFileUpload: false);

        // assert
        Assert.Throws<SyntaxException>(Act);
    }

    private static OperationExecutionNode GetLookupNode(OperationPlan plan)
        => plan.AllNodes
            .OfType<OperationExecutionNode>()
            .Single(t => !t.Source.IsRoot);

    private static async Task<IRequestExecutor> CreateExecutorAsync(ISourceSchemaClient client)
    {
        var services = new ServiceCollection();
        services.AddHttpClient();

        var builder = services
            .AddGraphQLRouterCore()
            .AddInMemoryConfiguration(ComposeSchemaDocument(SourceSchemaA, SourceSchemaB));

        builder.Services.AddSingleton<ISourceSchemaClientFactory>(new RecordingClientFactory(client));

        FusionSetupUtilities.Configure(
            builder,
            setup =>
            {
                setup.ClientConfigurationModifiers.Add(_ => new RecordingClientConfiguration("a"));
                setup.ClientConfigurationModifiers.Add(_ => new RecordingClientConfiguration("b"));
            });

        return await services.BuildRouterAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Answers every request with a canned response and records the requests it received, so a
    /// test can inspect what the execution nodes handed to the transport.
    /// </summary>
    private sealed class RecordingClient : ISourceSchemaClient
    {
        private static readonly byte[] s_books = """{"data":{"books":[{"id":"1"},{"id":"2"}]}}"""u8.ToArray();
        private static readonly byte[] s_book = """{"data":{"bookById":{"rating":"5"}}}"""u8.ToArray();
        private readonly List<SourceSchemaClientRequest> _requests = [];

        public IReadOnlyList<SourceSchemaClientRequest> Requests => _requests;

        public SourceSchemaClientCapabilities Capabilities => SourceSchemaClientCapabilities.None;

        public async IAsyncEnumerable<SourceSchemaResult> ExecuteAsync(
            OperationPlanContext context,
            SourceSchemaClientRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.Yield();
            _requests.Add(request);

            if (request.Variables.IsDefaultOrEmpty)
            {
                yield return CreateResult(context, s_books, CompactPath.Root, default);
                yield break;
            }

            foreach (var variables in request.Variables)
            {
                yield return CreateResult(context, s_book, variables.Path, variables.AdditionalPaths);
            }
        }

        private static SourceSchemaResult CreateResult(
            OperationPlanContext context,
            byte[] response,
            CompactPath path,
            CompactPathSegment additionalPaths)
        {
            var document = SourceResultDocument.Parse(
                context.MemorySource.GetNextArena(),
                response,
                response.Length);

            return new SourceSchemaResult(path, document, additionalPaths: additionalPaths);
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

    private sealed class RecordingClientFactory(ISourceSchemaClient client) : ISourceSchemaClientFactory
    {
        public bool CanHandle(ISourceSchemaClientConfiguration configuration)
            => configuration is RecordingClientConfiguration;

        public ISourceSchemaClient CreateClient(
            FusionSchemaDefinition schema,
            ISourceSchemaClientConfiguration configuration)
            => client;
    }

    private sealed class RecordingClientConfiguration(string name) : ISourceSchemaClientConfiguration
    {
        public string Name { get; } = name;

        public SupportedOperationType SupportedOperations { get; } = SupportedOperationType.Query;
    }
}
