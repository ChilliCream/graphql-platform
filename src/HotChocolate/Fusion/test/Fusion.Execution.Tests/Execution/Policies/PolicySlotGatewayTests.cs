using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution;

public sealed partial class PolicySlotGatewayTests : FusionTestBase
{
    [Theory]
    [InlineData("NULL")]
    [InlineData("ERROR")]
    public async Task ExecuteAsync_Should_MatchNodeDenial_When_SlotDenialUsesNullOrError(string onDenied)
    {
        // arrange
        var schema = CreateSchema(
            $$"""
            type Query {
              secret: String @policy(names: "CanReadSecret", onDenied: {{onDenied}})
              id: ID!
            }
            """);
        var slotExecutor = await CreateExecutorAsync(
            schema,
            new DenyPolicy("CanReadSecret"),
            new RecordingClient("""{"data":{"secret":"classified","id":"1"}}"""));
        var nodeExecutor = await CreateExecutorAsync(
            schema,
            new DenyIdRequirementPolicy("CanReadSecret"),
            new RecordingClient("""{"data":{"secret":"classified","id":"1"}}"""));

        // act
        await using var slotResult = await slotExecutor.ExecuteAsync(
            "{ secret }",
            TestContext.Current.CancellationToken);
        await using var nodeResult = await nodeExecutor.ExecuteAsync(
            "{ secret }",
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(NormalizeReasonId(nodeResult.ToJson()), NormalizeReasonId(slotResult.ToJson()));
        NormalizeReasonId(slotResult.ToJson()).MatchInlineSnapshot(
            onDenied == "NULL"
                ? """
                  {
                    "data": {
                      "secret": null
                    }
                  }
                  """
                : """
                  {
                    "errors": [
                      {
                        "message": "The current user is not authorized to access this resource.",
                        "path": [
                          "secret"
                        ],
                        "extensions": {
                          "code": "UNAUTHORIZED_FIELD_OR_TYPE",
                          "reasonId": "00000000-0000-0000-0000-000000000000"
                        }
                      }
                    ],
                    "data": {
                      "secret": null
                    }
                  }
                  """);
    }

    [Fact]
    public async Task ExecuteAsync_Should_NotDispatchMutation_When_RootPolicyDenies()
    {
        // arrange
        var client = new RecordingClient("""{"data":{"write":"changed"}}""");
        var executor = await CreateExecutorAsync(
            CreateSchema(
                """
                type Query { placeholder: String }
                type Mutation {
                  write: String @policy(names: "CanWrite", onDenied: NULL)
                }
                """),
            new DenyPolicy("CanWrite"),
            client);

        // act
        await using var result = await executor.ExecuteAsync(
            "mutation { write }",
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(0, client.ExecuteCount);
        result.ToJson().MatchInlineSnapshot(
            """
            {
              "data": {
                "write": null
              }
            }
            """);
    }

    [Fact]
    public async Task ExecuteAsync_Should_SkipDeniedRootAndKeepSibling_When_QueryPolicyDenies()
    {
        // arrange
        var protectedClient = new RecordingClient("""{"data":{"secret":"classified"}}""");
        var publicClient = new RecordingClient("""{"data":{"public":"visible"}}""");
        var executor = await CreateExecutorAsync(
            [
                """
                # name: protected
                enum PolicyDenialBehavior { NULL ERROR ABORT }
                directive @policy(names: [[String!]!]!, onDenied: PolicyDenialBehavior) repeatable on OBJECT | FIELD_DEFINITION
                type Query {
                  secret: String @policy(names: "CanReadSecret", onDenied: NULL)
                }
                """,
                """
                # name: public
                type Query {
                  public: String
                }
                """
            ],
            new DenyPolicy("CanReadSecret"),
            ("protected", protectedClient),
            ("public", publicClient));

        // act
        await using var result = await executor.ExecuteAsync(
            "{ secret public }",
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(0, protectedClient.ExecuteCount);
        Assert.Equal(1, publicClient.ExecuteCount);
        result.ToJson().MatchInlineSnapshot(
            """
            {
              "data": {
                "secret": null,
                "public": "visible"
              }
            }
            """);
    }

    [Fact]
    public async Task ExecuteAsync_Should_ForwardSameSlotDocument_When_PartialNodePolicyDenies()
    {
        // arrange
        var client = new RecordingClient("""{"data":{"product":{"id":"1","name":"Ada","secret":"classified"}}}""");
        var executor = await CreateExecutorAsync(
            CreateSchema(
                """
                type Query { product: Product }
                type Product {
                  id: ID!
                  name: String
                  secret: String @policy(names: "CanReadSecret", onDenied: NULL)
                }
                """),
            new RolePolicy("CanReadSecret"),
            client);
        var allowedRequest = CreateRequest("{ product { name secret } }", allowed: true);
        var deniedRequest = CreateRequest("{ product { name secret } }", allowed: false);

        // act
        await using var allowed = await executor.ExecuteAsync(
            allowedRequest,
            TestContext.Current.CancellationToken);
        await using var denied = await executor.ExecuteAsync(
            deniedRequest,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(2, client.Requests.Count);
        var allowedDispatch = client.Requests[0];
        var deniedDispatch = client.Requests[1];
        Assert.Equal(allowedDispatch.Operation, deniedDispatch.Operation);
        Encoding.UTF8.GetString(allowedDispatch.Operation).MatchInlineSnapshot(
            """
            query Op_c60cf1cd_1($__fusion_policy_0: Boolean!) {
              product {
                name
                ... @include(if: $__fusion_policy_0) {
                  secret
                }
              }
            }
            """);
        new object?[]
        {
            GetPolicyVariableValue(allowedDispatch),
            GetPolicyVariableValue(deniedDispatch)
        }.MatchInlineSnapshots(["true", "false"]);
        new[] { allowed.ToJson(), denied.ToJson() }.MatchInlineSnapshots(
        [
            """
            {
              "data": {
                "product": {
                  "name": "Ada",
                  "secret": "classified"
                }
              }
            }
            """,
            """
            {
              "data": {
                "product": {
                  "name": "Ada",
                  "secret": null
                }
              }
            }
            """
        ]);
    }

    [Fact]
    public async Task ExecuteAsync_Should_PreserveListCardinality_When_EachObjectPolicyDenies()
    {
        // arrange
        var client = new RecordingClient(
            """{"data":{"products":[{"id":"1"},{"id":"2"},{"id":"3"}]}}""");
        var executor = await CreateExecutorAsync(
            CreateSchema(
                """
                type Query { products: [Product] }
                type Product @policy(names: "CanReadProduct", onDenied: NULL) { id: ID! }
                """),
            new DenyIdRequirementPolicy("CanReadProduct"),
            client);

        // act
        await using var result = await executor.ExecuteAsync(
            "{ products { id } }",
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(1, client.ExecuteCount);
        result.ToJson().MatchInlineSnapshot(
            """
            {
              "data": {
                "products": [
                  null,
                  null,
                  null
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task ExecuteAsync_Should_DenyProductAndAllowBook_When_AbstractNodePolicyApplies()
    {
        // arrange
        var schema = CreateSchema(
            """
                type Query { node(id: ID!): Node @lookup }
                interface Node { id: ID! }
                type Product implements Node @policy(names: "CanReadProduct", onDenied: NULL) { id: ID! }
                type Book implements Node { id: ID! }
            """);
        var productClient = new RecordingClient(
            """{"data":{"node":{"__typename":"Product","id":"product"}}}""");
        var bookClient = new RecordingClient(
            """{"data":{"node":{"__typename":"Book","id":"book"}}}""");
        var productExecutor = await CreateExecutorAsync(
            schema,
            new DenyPolicy("CanReadProduct"),
            productClient);
        var bookExecutor = await CreateExecutorAsync(
            schema,
            new DenyPolicy("CanReadProduct"),
            bookClient);

        // act
        await using var product = await productExecutor.ExecuteAsync(
            "{ node(id: \"UHJvZHVjdDox\") { id } }",
            TestContext.Current.CancellationToken);
        await using var book = await bookExecutor.ExecuteAsync(
            "{ node(id: \"Qm9vazox\") { id } }",
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal((1, 1), (productClient.ExecuteCount, bookClient.ExecuteCount));
        new[] { product.ToJson(), book.ToJson() }.MatchInlineSnapshots(
        [
            """
            {
              "data": {
                "node": null
              }
            }
            """,
            """
            {
              "data": {
                "node": {
                  "id": "book"
                }
              }
            }
            """
        ]);
    }

    [Fact]
    public async Task ExecuteAsync_Should_EvaluateOnePolicyName_When_VariableBatchHasMultipleItems()
    {
        // arrange
        var policy = new CountingPolicy("CanReadSecret");
        var executor = await CreateExecutorAsync(
            CreateSchema(
                """
                type Query { secret: String @policy(names: "CanReadSecret", onDenied: NULL) }
                """),
            policy,
            new RecordingClient("""{"data":{"secret":"classified"}}"""));
        using var values = JsonDocument.Parse("""[{"include":true},{"include":true},{"include":false}]""");
        var request = VariableBatchRequest.FromSourceText(
            "query($include: Boolean!) { secret @include(if: $include) }",
            values);

        // act
        await using var result = await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(1, policy.EvaluationCount);
        Assert.IsType<OperationResultBatch>(result).Results.Select(result => result.ToJson()).ToArray()
            .MatchInlineSnapshots(
            [
                """
                {
                  "data": {
                    "secret": "classified"
                  }
                }
                """,
                """
                {
                  "data": {
                    "secret": "classified"
                  }
                }
                """,
                """
                {
                  "data": {}
                }
                """
            ]);
    }

    private static string CreateSchema(string typeDefinitions)
        => $$"""
        # name: a
        enum PolicyDenialBehavior { NULL ERROR ABORT }
        directive @policy(names: [[String!]!]!, onDenied: PolicyDenialBehavior) repeatable on OBJECT | FIELD_DEFINITION
        {{typeDefinitions}}
        """;

    private static string NormalizeReasonId(string json)
        => ReasonIdRegex().Replace(json, "00000000-0000-0000-0000-000000000000");

    [GeneratedRegex("[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}")]
    private static partial Regex ReasonIdRegex();

    private static IOperationRequest CreateRequest(string document, bool allowed)
        => OperationRequestBuilder.New()
            .SetDocument(document)
            .SetUser(
                allowed
                    ? new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, "reader")], "test"))
                    : new ClaimsPrincipal(new ClaimsIdentity()))
            .Build();

    private static async Task<IRequestExecutor> CreateExecutorAsync(
        string schema,
        IPolicy policy,
        RecordingClient client)
        => await CreateExecutorAsync([schema], policy, ("a", client));

    private static async Task<IRequestExecutor> CreateExecutorAsync(
        string[] schemas,
        IPolicy policy,
        params (string Name, RecordingClient Client)[] clients)
    {
        var services = new ServiceCollection();
        services.AddHttpClient();
        var builder = services.AddGraphQLGateway();
        builder.AddInMemoryConfiguration(ComposeSchemaDocument(schemas));
        builder.ConfigureSchemaServices(
            (_, schemaServices) => schemaServices.AddSingleton<IPolicyProvider>(new TestPolicyProvider(policy)));
        builder.Services.AddSingleton<ISourceSchemaClientFactory>(new ClientFactory(clients));
        FusionSetupUtilities.Configure(
            builder,
            setup =>
            {
                foreach (var (name, _) in clients)
                {
                    setup.ClientConfigurationModifiers.Add(_ => new ClientConfiguration(name));
                }
            });
        return await services.BuildGatewayAsync(TestContext.Current.CancellationToken);
    }

    private static bool GetPolicyVariableValue(RecordedRequest request)
    {
        if (request.Variables.Length is not 1)
        {
            throw new InvalidOperationException("Expected exactly one variable set.");
        }

        using var document = JsonDocument.Parse(request.Variables[0]);
        var root = document.RootElement;

        if (root.ValueKind is not JsonValueKind.Object
            || root.EnumerateObject().Count() is not 1
            || !root.TryGetProperty("__fusion_policy_0", out var value)
            || (value.ValueKind is not JsonValueKind.True and not JsonValueKind.False))
        {
            throw new InvalidOperationException("Expected the policy variable set.");
        }

        return value.GetBoolean();
    }

    private static byte[] CopyJsonSegment(JsonSegment segment)
    {
        var sequence = segment.AsSequence();
        var copy = new byte[checked((int)sequence.Length)];
        var offset = 0;

        foreach (var memory in sequence)
        {
            memory.Span.CopyTo(copy.AsSpan(offset));
            offset += memory.Length;
        }

        return copy;
    }

    private sealed class DenyPolicy(string name) : IPolicy
    {
        public string Name => name;

        public PolicyRequirements Requirements => PolicyRequirements.Empty;

        public ValueTask EvaluateAsync(IPolicyContext context, CancellationToken cancellationToken)
        {
            context.Deny(0, "denied by test policy");
            return ValueTask.CompletedTask;
        }
    }

    private sealed class RolePolicy(string name) : IPolicy
    {
        public string Name => name;

        public PolicyRequirements Requirements => PolicyRequirements.Empty;

        public ValueTask EvaluateAsync(IPolicyContext context, CancellationToken cancellationToken)
        {
            if (!context.User.IsInRole("reader"))
            {
                context.Deny(0, "denied by test policy");
            }

            return ValueTask.CompletedTask;
        }
    }

    private sealed class DenyIdRequirementPolicy(string name) : IPolicy
    {
        private static readonly PolicyRequirements s_requirements =
            new() { Resource = Utf8GraphQLParser.Syntax.ParseSelectionSet("{ id }") };

        public string Name => name;

        public PolicyRequirements Requirements => s_requirements;

        public ValueTask EvaluateAsync(IPolicyContext context, CancellationToken cancellationToken)
        {
            for (var i = 0; i < context.Selection!.Entities.Length; i++)
            {
                context.Deny(i, "denied by test policy");
            }

            return ValueTask.CompletedTask;
        }
    }

    private sealed class CountingPolicy(string name) : IPolicy
    {
        private int _evaluationCount;

        public string Name => name;

        public PolicyRequirements Requirements => PolicyRequirements.Empty;

        public int EvaluationCount => Volatile.Read(ref _evaluationCount);

        public ValueTask EvaluateAsync(IPolicyContext context, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _evaluationCount);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class RecordingClient : ISourceSchemaClient
    {
        private readonly Func<SourceSchemaClientRequest, byte[]> _payloadFactory;

        public RecordingClient(string payload)
            : this(_ => payload)
        {
        }

        public RecordingClient(Func<SourceSchemaClientRequest, string> payloadFactory)
            => _payloadFactory = request => Encoding.UTF8.GetBytes(payloadFactory(request));

        public int ExecuteCount { get; private set; }

        public List<RecordedRequest> Requests { get; } = [];

        public SourceSchemaClientCapabilities Capabilities => SourceSchemaClientCapabilities.None;

        public async IAsyncEnumerable<SourceSchemaResult> ExecuteAsync(
            OperationPlanContext context,
            SourceSchemaClientRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            ExecuteCount++;
            var variables = new byte[request.Variables.Length][];

            for (var i = 0; i < request.Variables.Length; i++)
            {
                variables[i] = CopyJsonSegment(request.Variables[i].Values);
            }

            Requests.Add(
                new RecordedRequest(
                    request.OperationSourceText.Value.Span.ToArray(),
                    variables));
            var payload = _payloadFactory(request);
            var arena = context.MemorySource.GetNextArena();
            var document = SourceResultDocument.Parse(arena, payload, payload.Length);
            await Task.Yield();
            yield return new SourceSchemaResult(CompactPath.Root, document);
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

    private sealed record RecordedRequest(byte[] Operation, byte[][] Variables);

    private sealed class ClientFactory(params (string Name, RecordingClient Client)[] clients)
        : ISourceSchemaClientFactory
    {
        public bool CanHandle(ISourceSchemaClientConfiguration configuration)
            => configuration is ClientConfiguration;

        public ISourceSchemaClient CreateClient(
            FusionSchemaDefinition schema,
            ISourceSchemaClientConfiguration configuration)
            => clients.Single(t => t.Name == configuration.Name).Client;
    }

    private sealed class ClientConfiguration(string name) : ISourceSchemaClientConfiguration
    {
        public string Name { get; } = name;

        public SupportedOperationType SupportedOperations => SupportedOperationType.All;
    }
}
