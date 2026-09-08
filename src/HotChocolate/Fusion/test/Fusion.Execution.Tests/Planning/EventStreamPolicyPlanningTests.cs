using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Execution.Nodes.Serialization;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Fusion.Planning;

/// <summary>
/// Covers repo-ctf.11: a data-bearing policy directly on a subscription root's own (concrete)
/// payload type is planned as a per-event resource slot, not a PolicyExecutionNode, when its
/// requirement is derivable from the composed <c>@eventStream</c> message projection.
/// </summary>
public sealed class EventStreamPolicyPlanningTests : FusionTestBase
{
    [Fact]
    public void CreatePlan_Should_UseEventResourceSlot_When_RootPayloadPolicyRequirementIsDerivable()
    {
        // arrange
        var schema = CreatePayloadPolicySchema(message: "{ id title }", requirement: "{ title }");

        // act
        var plan = PlanOperation(
            schema,
            """
            subscription {
              bookChanged {
                id
                title
              }
            }
            """);

        // assert
        Assert.Empty(plan.AllNodes.OfType<PolicyExecutionNode>());
        Assert.IsType<EventStreamExecutionNode>(Assert.Single(plan.RootNodes));
        Assert.Single(plan.PolicySlots);
        MatchSnapshot(plan);
    }

    [Fact]
    public void CreatePlan_Should_RejectRequirementPolicy_When_NotDerivableFromEventMessage()
    {
        // arrange
        var schema = CreatePayloadPolicySchema(message: "{ id }", requirement: "{ title }");

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => PlanOperation(schema, "subscription { bookChanged { id title } }"));

        // assert
        Assert.Equal(
            "Policy 'CanReadBook' has a requirement that cannot be derived from the composed "
            + "event message projection; only fields selected by the @eventStream message are "
            + "available as the per-event resource on a subscription root field.",
            exception.Message);
    }

    [Fact]
    public void CreatePlan_Should_UseEventResourceSlot_When_RequirementIsCoveredByNestedField()
    {
        // arrange
        // The requirement's `narrator { name }` matches the message's own nested selection,
        // exercising the recursive field-coverage check below the coordinate's own type.
        var schema = CreateNestedPayloadPolicySchema(
            message: "{ id narrator { name } }",
            requirement: "{ narrator { name } }");

        // act
        var plan = PlanOperation(schema, "subscription { bookChanged { id } }");

        // assert
        Assert.Empty(plan.AllNodes.OfType<PolicyExecutionNode>());
        Assert.Single(plan.PolicySlots);
    }

    [Fact]
    public void CreatePlan_Should_RejectRequirementPolicy_When_NestedFieldIsMissing()
    {
        // arrange
        // The message's `narrator` selection only exposes `id`; the requirement's
        // `narrator { name }` has no matching field there.
        var schema = CreateNestedPayloadPolicySchema(
            message: "{ id narrator { id } }",
            requirement: "{ narrator { name } }");

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => PlanOperation(schema, "subscription { bookChanged { id } }"));

        // assert
        Assert.Equal(
            "Policy 'CanReadBook' has a requirement that cannot be derived from the composed "
            + "event message projection; only fields selected by the @eventStream message are "
            + "available as the per-event resource on a subscription root field.",
            exception.Message);
    }

    [Fact]
    public void JsonParser_Should_RoundTripEventResourceSlot()
    {
        // arrange
        var schema = CreatePayloadPolicySchema(message: "{ id title }", requirement: "{ title }");
        var plan = PlanOperation(
            schema,
            """
            subscription {
              bookChanged {
                id
                title
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
                new FieldMapPooledObjectPolicy()));
        var parser = new JsonOperationPlanParser(compiler);

        // act
        var parsedPlan = parser.Parse(buffer.WrittenMemory);

        using var roundTripBuffer = new PooledArrayWriter();
        formatter.Format(roundTripBuffer, parsedPlan);

        var original = Encoding.UTF8.GetString(buffer.WrittenSpan);
        var roundTripped = Encoding.UTF8.GetString(roundTripBuffer.WrittenSpan);

        // assert
        Assert.Empty(parsedPlan.AllNodes.OfType<PolicyExecutionNode>());
        Assert.Single(parsedPlan.PolicySlots);
        Assert.Equal(original, roundTripped);
    }

    [Fact]
    public void YamlFormatter_Should_FormatEventResourceSlot_When_PlanHasNoPolicyExecutionNode()
    {
        // arrange
        var schema = CreatePayloadPolicySchema(message: "{ id title }", requirement: "{ title }");
        var plan = PlanOperation(
            schema,
            """
            subscription {
              bookChanged {
                id
                title
              }
            }
            """);

        // act
        var yaml = new YamlOperationPlanFormatter().Format(plan);

        // assert
        Assert.Contains("CanReadBook", yaml, StringComparison.Ordinal);
        Assert.Empty(plan.AllNodes.OfType<PolicyExecutionNode>());
    }

    private static FusionSchemaDefinition CreatePayloadPolicySchema(string message, string requirement)
        => CreateSchema(
            ComposeSchemaDocument(
                $$"""
                # name: a
                enum PolicyDenialBehavior { NULL ERROR ABORT }
                directive @policy(names: [[String!]!]!, onDenied: PolicyDenialBehavior)
                  repeatable on OBJECT | FIELD_DEFINITION

                type Query {
                  bookById(id: ID!): Book
                }

                type Subscription {
                  bookChanged: Book
                    @eventStream(message: "{{message}}", topics: ["book.changed"])
                }

                type Book @policy(names: "CanReadBook") {
                  id: ID!
                  title: String!
                }
                """),
            new TestPolicy("CanReadBook", Utf8GraphQLParser.Syntax.ParseSelectionSet(requirement)));

    private static FusionSchemaDefinition CreateNestedPayloadPolicySchema(
        string message,
        string requirement)
        => CreateSchema(
            ComposeSchemaDocument(
                $$"""
                # name: a
                enum PolicyDenialBehavior { NULL ERROR ABORT }
                directive @policy(names: [[String!]!]!, onDenied: PolicyDenialBehavior)
                  repeatable on OBJECT | FIELD_DEFINITION

                type Query {
                  bookById(id: ID!): Book
                }

                type Subscription {
                  bookChanged: Book
                    @eventStream(message: "{{message}}", topics: ["book.changed"])
                }

                type Book @policy(names: "CanReadBook") {
                  id: ID!
                  narrator: Person
                }

                type Person {
                  name: String!
                  id: ID!
                }
                """),
            new TestPolicy("CanReadBook", Utf8GraphQLParser.Syntax.ParseSelectionSet(requirement)));

    private static FusionSchemaDefinition CreateSchema(
        DocumentNode schemaDocument,
        params IPolicy[] policies)
    {
        var services = new ServiceCollection()
            .AddSingleton<IPolicyProvider>(_ => new TestPolicyProvider(policies))
            .BuildServiceProvider();

        return FusionSchemaDefinition.Create(schemaDocument, services);
    }
}
