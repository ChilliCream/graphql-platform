using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Execution.Nodes.Serialization;
using HotChocolate.Fusion.Execution.Rewriters;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Fusion.Planning;

public sealed class PolicyPlanningTests : FusionTestBase
{
    [Fact]
    public void CreatePlan_Should_AddPolicyNode_When_OperationStepContainsPolicyTargets()
    {
        // arrange
        var schema = CreatePolicySchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query($includeName: Boolean!) {
              product {
                id
                name @include(if: $includeName)
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void JsonParser_Should_RoundTripPolicyNode()
    {
        var schema = CreatePolicySchema();
        var plan = PlanOperation(
            schema,
            """
            {
              product {
                id
                name
              }
            }
            """);

        using var buffer = new PooledArrayWriter();
        var formatter = new JsonOperationPlanFormatter(
            new JsonWriterOptions
            {
                Indented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        formatter.Format(buffer, plan);

        var compiler = new OperationCompiler(
            schema,
            new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
                new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>()));
        var parser = new JsonOperationPlanParser(compiler);

        var parsedPlan = parser.Parse(buffer.WrittenMemory);

        using var roundTripBuffer = new PooledArrayWriter();
        formatter.Format(roundTripBuffer, parsedPlan);

        var original = Encoding.UTF8.GetString(buffer.WrittenSpan);
        var roundTripped = Encoding.UTF8.GetString(roundTripBuffer.WrittenSpan);
        Assert.Equal(original, roundTripped);
    }

    [Fact]
    public void JsonParser_Should_RoundTripPolicyRequirements()
    {
        var schema = CreateRequirementPolicySchema();
        var pool = new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
            new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>());
        var operationDocument = Utf8GraphQLParser.Parse(
            """
            {
              secret
            }
            """);
        var rewritten = new DocumentRewriter(schema).RewriteDocument(
            operationDocument,
            operationName: null);
        var operation = rewritten.Definitions.OfType<OperationDefinitionNode>().Single();
        var compiler = new OperationCompiler(schema, pool);
        var planner = new OperationPlanner(
            schema,
            compiler,
            OperationPlannerOptions.Default,
            [new TestPolicyDefinition("CanReadSecret", "{ role }")]);
        var plan = planner.CreatePlan(
            "123456789101112",
            "123456789101112",
            "123456789101112",
            operation,
            TestContext.Current.CancellationToken);

        using var buffer = new PooledArrayWriter();
        var formatter = new JsonOperationPlanFormatter(
            new JsonWriterOptions
            {
                Indented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        formatter.Format(buffer, plan);

        var parser = new JsonOperationPlanParser(compiler);
        var parsedPlan = parser.Parse(buffer.WrittenMemory);

        using var roundTripBuffer = new PooledArrayWriter();
        formatter.Format(roundTripBuffer, parsedPlan);

        Assert.Equal(
            Encoding.UTF8.GetString(buffer.WrittenSpan),
            Encoding.UTF8.GetString(roundTripBuffer.WrittenSpan));
        var target = Assert.Single(
            parsedPlan.AllNodes
                .OfType<PolicyExecutionNode>()
                .SelectMany(t => t.Targets.ToArray()),
            t => t.Policies.Any(p => p.Name == "CanReadSecret"));
        var requirement = Assert.Single(target.Requirements);
        Assert.Equal("CanReadSecret", requirement.PolicyName);
        Assert.Equal("{ role }", requirement.SelectionSet.ToString(indented: false));
        Assert.Contains(
            "requirements: { role }",
            new YamlOperationPlanFormatter().Format(parsedPlan),
            StringComparison.Ordinal);
    }

    [Fact]
    public void CreatePlan_Should_GuardDownstreamLookup_When_ProtectedEntityIsFetchedFirst()
    {
        // arrange
        var schema = ComposeSchema(
            """
            enum PolicyDenialBehavior { NULL ERROR ABORT }

            directive @policy(name: String!, onDenied: PolicyDenialBehavior! = NULL)
              repeatable on OBJECT | INTERFACE | FIELD_DEFINITION

            type Query {
              topProducts: [Product!]
            }

            type Product @key(fields: "id") @policy(name: "CanReadProduct") {
              id: ID!
              name: String!
            }
            """,
            """
            type Query {
              productById(id: ID!): Product @lookup @internal
            }

            type Product {
              id: ID!
              price: Float!
            }
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              topProducts {
                name
                price
              }
            }
            """);

        // assert
        var producer = Assert.Single(
            plan.AllNodes.OfType<OperationExecutionNode>(),
            t => t.SchemaName == "a");
        var downstream = Assert.Single(
            plan.AllNodes.OfType<OperationExecutionNode>(),
            t => t.SchemaName == "b");
        var policy = Assert.Single(
            plan.AllNodes.OfType<PolicyExecutionNode>(),
            t => t.Dependencies.ToArray().Any(d => ReferenceEquals(d, producer)));

        Assert.Contains(downstream.Dependencies.ToArray(), d => ReferenceEquals(d, policy));
        Assert.Contains(policy.Dependents.ToArray(), d => ReferenceEquals(d, downstream));
    }

    private static FusionSchemaDefinition CreatePolicySchema()
        => CreateCompositeSchema(
            """
            schema {
              query: Query
            }

            type Query
              @fusion__type(schema: A)
              @fusion__policy(name: "CanReadQuery", onDenied: ERROR) {
              product: Product
                @fusion__field(schema: A)
                @fusion__policy(name: "CanReadProductField", onDenied: ABORT)
            }

            type Product
              @fusion__type(schema: A)
              @fusion__policy(name: "CanReadProductObject") {
              id: ID! @fusion__field(schema: A)
              name: String
                @fusion__field(schema: A)
                @fusion__policy(name: "CanReadName", onDenied: ERROR)
            }

            enum fusion__Schema {
              A @fusion__schema_metadata(name: "A")
            }
            """);

    private static FusionSchemaDefinition CreateRequirementPolicySchema()
        => CreateCompositeSchema(
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) {
              secret: String
                @fusion__field(schema: A)
                @fusion__policy(name: "CanReadSecret")
              role: String @fusion__field(schema: A)
            }

            enum fusion__Schema {
              A @fusion__schema_metadata(name: "A")
            }
            """);

    private sealed class TestPolicyDefinition(string name, string requirements)
        : IAuthorizationPolicyDefinition
    {
        public string Name { get; } = name;

        public SelectionSetNode? Requirements { get; } =
            Utf8GraphQLParser.Syntax.ParseSelectionSet(requirements);
    }
}
