using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using HotChocolate.Buffers;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Execution.Nodes.Serialization;
using HotChocolate.Fusion.Execution.Rewriters;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;
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
        var planner = new OperationPlanner(schema, compiler);
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
            t => t.Policies.Any(
                p => p.Groups.Any(g => g.Contains("CanReadSecret", StringComparer.Ordinal))));
        var requirement = Assert.Single(target.Requirements);
        Assert.Equal("CanReadSecret", requirement.PolicyName);
        Assert.Equal("{ role }", requirement.SelectionSet.ToString(indented: false));
        Assert.Contains(
            "selectionSet: { role }",
            new YamlOperationPlanFormatter().Format(parsedPlan),
            StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(
        "[]",
        "A policy in the operation plan must contain at least one policy name group.")]
    [InlineData(
        "[[]]",
        "A policy name group in the operation plan must contain at least one policy name.")]
    [InlineData(
        "[[1]]",
        "A policy name in the operation plan must be a string.")]
    public void JsonParser_Should_RejectPolicyNode_When_PolicyNamesAreMalformed(
        string namesJson,
        string expectedMessage)
    {
        // arrange
        // An empty group would be vacuously satisfied and grant access without
        // evaluating any policy, so the parser must reject these shapes.
        var schema = CreatePolicySchema();
        var plan = PlanOperation(
            schema,
            """
            {
              product {
                id
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

        var json = JsonNode.Parse(buffer.WrittenSpan)!;
        var policyNode = json["nodes"]!
            .AsArray()
            .Select(t => t!.AsObject())
            .First(t => t["type"]?.GetValue<string>() is "Policy");
        var target = policyNode["targets"]!.AsArray()[0]!.AsObject();
        target["policies"]!.AsArray()[0]!.AsObject()["names"] = JsonNode.Parse(namesJson);
        var planSource = Encoding.UTF8.GetBytes(json.ToJsonString());

        var compiler = new OperationCompiler(
            schema,
            new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
                new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>()));
        var parser = new JsonOperationPlanParser(compiler);

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => parser.Parse(planSource));

        // assert
        Assert.Equal(expectedMessage, exception.Message);
    }

    [Fact]
    public void CreatePlan_Should_GuardDownstreamLookup_When_ProtectedEntityIsFetchedFirst()
    {
        // arrange
        var schema = CreateSchema(
            ComposeSchemaDocument(
                """
                enum PolicyDenialBehavior { NULL ERROR ABORT }

                directive @policy(names: [[String!]!]!, onDenied: PolicyDenialBehavior! = NULL)
                  repeatable on OBJECT | INTERFACE | FIELD_DEFINITION

                type Query {
                  topProducts: [Product!]
                }

                type Product @key(fields: "id") @policy(names: "CanReadProduct") {
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
                """),
            new TestAuthorizationPolicy("CanReadProduct"));

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
        => CreateSchema(
            """
            schema {
              query: Query
            }

            type Query
              @fusion__type(schema: A)
              @fusion__policy(names: "CanReadQuery", onDenied: ERROR) {
              product: Product
                @fusion__field(schema: A)
                @fusion__policy(names: "CanReadProductField", onDenied: ABORT)
            }

            type Product
              @fusion__type(schema: A)
              @fusion__policy(names: "CanReadProductObject") {
              id: ID! @fusion__field(schema: A)
              name: String
                @fusion__field(schema: A)
                @fusion__policy(names: [["CanReadName", "CanAudit"], ["CanAdmin"]], onDenied: ERROR)
            }

            enum fusion__Schema {
              A @fusion__schema_metadata(name: "A")
            }
            """,
            new TestAuthorizationPolicy("CanReadQuery"),
            new TestAuthorizationPolicy("CanReadProductField"),
            new TestAuthorizationPolicy("CanReadProductObject"),
            new TestAuthorizationPolicy("CanReadName"),
            new TestAuthorizationPolicy("CanAudit"),
            new TestAuthorizationPolicy("CanAdmin"));

    private static FusionSchemaDefinition CreateRequirementPolicySchema()
        => CreateSchema(
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) {
              secret: String
                @fusion__field(schema: A)
                @fusion__policy(names: "CanReadSecret")
              role: String @fusion__field(schema: A)
            }

            enum fusion__Schema {
              A @fusion__schema_metadata(name: "A")
            }
            """,
            new TestAuthorizationPolicy(
                "CanReadSecret",
                Utf8GraphQLParser.Syntax.ParseSelectionSet("{ role }")));

    private static FusionSchemaDefinition CreateSchema(
        string schemaText,
        params IAuthorizationPolicy[] policies)
        => CreateSchema(Utf8GraphQLParser.Parse(schemaText), policies);

    private static FusionSchemaDefinition CreateSchema(
        DocumentNode schemaDocument,
        params IAuthorizationPolicy[] policies)
    {
        var services = new ServiceCollection()
            .AddSingleton<IAuthorizationPolicyProvider>(
                _ => new TestAuthorizationPolicyProvider(policies))
            .BuildServiceProvider();

        return FusionSchemaDefinition.Create(schemaDocument, services);
    }
}
