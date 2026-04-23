using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using HotChocolate.Buffers;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Execution.Nodes.Serialization;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Fusion.Execution.Serialization;

public class JsonOperationPlanSerializationTests : FusionTestBase
{
    [Fact]
    public void Parse_Plan()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();
        var originalPlan = PlanOperation(
            compositeSchema,
            """
            {
                productBySlug(slug: "1") {
                    ... Product
                }
            }

            fragment Product on Product {
                id
                name
                estimatedDelivery(postCode: "12345")
            }
            """);

        using var buffer = new PooledArrayWriter();
        var formatter = new JsonOperationPlanFormatter(
            new JsonWriterOptions
            {
                Indented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        formatter.Format(buffer, originalPlan);

        // act
        var compiler = new OperationCompiler(
            compositeSchema,
            new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
                new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>()));
        var parser = new JsonOperationPlanParser(compiler);
        var parsedPlan = parser.Parse(buffer.WrittenMemory);

        // assert
        formatter.Format(parsedPlan).MatchInlineSnapshot(Encoding.UTF8.GetString(buffer.WrittenSpan));
    }

    [Fact]
    public void Parse_Plan_Uses_SelectionSet_Syntax_When_Present()
    {
        // arrange
        // Inject a custom selection set string into the formatted plan, then
        // parse it back to confirm the parser preserves the syntax.
        var compositeSchema = CreateCompositeSchema();
        var originalPlan = PlanOperation(
            compositeSchema,
            """
            {
                productBySlug(slug: "1") {
                    id
                    name
                }
            }
            """);

        var formatter = new JsonOperationPlanFormatter(
            new JsonWriterOptions
            {
                Indented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        var json = JsonNode.Parse(formatter.Format(originalPlan))!;
        var operationNode = json["nodes"]!
            .AsArray()
            .Select(t => t!.AsObject())
            .First(t => t["type"]?.GetValue<string>() is "Operation");
        var operationNodeId = operationNode["id"]!.GetValue<int>();
        operationNode["resultSelectionSet"] = "{ __typename }";
        var planSource = Encoding.UTF8.GetBytes(json.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        var compiler = new OperationCompiler(
            compositeSchema,
            new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
                new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>()));
        var parser = new JsonOperationPlanParser(compiler);

        // act
        var parsedPlan = parser.Parse(planSource);

        // assert
        var parsedOperationNode = parsedPlan.AllNodes
            .OfType<OperationExecutionNode>()
            .Single(t => t.Id == operationNodeId);
        Assert.Equal("{ __typename }", parsedOperationNode.ResultSelectionSet.ToString(indented: false));
    }

    [Fact]
    public void Parse_Plan_With_Node()
    {
        // arrange
        var source1 = new TestSourceSchema(
            """
            type Query {
              node(id: ID!): Node @lookup
              authorById(id: ID!): Author @lookup
            }

            interface Node {
              id: ID!
            }

            type Discussion implements Node {
              id: ID!
              title: String!
            }

            type Author implements Node {
              id: ID!
              name: String!
            }
            """);

        var compositeSchema = ComposeSchema(source1);

        var originalPlan = PlanOperation(
            compositeSchema,
            """
            {
              a: node(id: "abc") {
                id
                ... on Discussion {
                  title
                }
                ... on Author {
                  name
                }
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
        formatter.Format(buffer, originalPlan);

        // act
        var compiler = new OperationCompiler(
            compositeSchema,
            new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
                new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>()));
        var parser = new JsonOperationPlanParser(compiler);
        var parsedPlan = parser.Parse(buffer.WrittenMemory);

        // assert
        var parsedPlanFormatted = formatter.Format(parsedPlan);
        parsedPlanFormatted.MatchInlineSnapshot(Encoding.UTF8.GetString(buffer.WrittenSpan));
    }

    [Fact]
    public void Parse_Plan_Preserves_DeliveryGroup_Identity_Across_Plan_And_SubPlans()
    {
        // arrange
        // Two sibling @defer fragments share a field (email) plus a nested @defer
        // adds a parent chain. Round-trip must restore canonical DeferUsage instances.
        var schema = ComposeSchema(
            """
            # name: a
            type Query {
                user(id: ID!): User @lookup
            }

            type User @key(fields: "id") {
                id: ID!
                name: String!
            }
            """,
            """
            # name: b
            type Query {
                userById(id: ID!): User @lookup
            }

            type User @key(fields: "id") {
                id: ID!
                email: String!
                address: String!
            }
            """);

        var originalPlan = PlanOperation(
            schema,
            """
            query {
                user(id: "1") {
                    name
                    ... @defer(label: "contact") {
                        email
                        ... @defer(label: "nested") {
                            address
                        }
                    }
                    ... @defer(label: "location") {
                        email
                    }
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
        formatter.Format(buffer, originalPlan);

        // act
        var compiler = new OperationCompiler(
            schema,
            new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
                new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>()));
        var parser = new JsonOperationPlanParser(compiler);
        var parsedPlan = parser.Parse(buffer.WrittenMemory);

        // assert
        Encoding.UTF8.GetString(buffer.WrittenSpan).MatchSnapshot();
        Assert.All(
            parsedPlan.DeferredSubPlans,
            p => Assert.All(
                p.DeliveryGroups,
                g => Assert.Same(parsedPlan.DeliveryGroups.Single(d => d.Id == g.Id), g)));
        Assert.All(
            parsedPlan.DeliveryGroups.Where(g => g.Parent is not null),
            g => Assert.Same(parsedPlan.DeliveryGroups.Single(d => d.Id == g.Parent!.Id), g.Parent));
    }

    [Fact]
    public void Parse_Plan_Preserves_ParentDependencies_On_Deferred_SubPlan_Nodes()
    {
        // arrange
        // Same-subgraph hoist injects the key into the parent op so the
        // deferred sub-plan node carries a ParentStepRef on its plan step
        // and a {"parentNodeId": N} entry in its serialized dependencies.
        var schema = ComposeSchema(
            """
            # name: a
            type Query {
                user(id: ID!): User @lookup
            }

            type User @key(fields: "id") {
                id: ID!
                name: String!
            }
            """,
            """
            # name: b
            type Query {
                userById(id: ID!): User @lookup
            }

            type User @key(fields: "id") {
                id: ID!
                email: String!
            }
            """);

        var originalPlan = PlanOperation(
            schema,
            """
            query {
                user(id: "1") {
                    name
                    ... @defer(label: "contact") {
                        email
                    }
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
        formatter.Format(buffer, originalPlan);

        var compiler = new OperationCompiler(
            schema,
            new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
                new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>()));
        var parser = new JsonOperationPlanParser(compiler);

        // act
        var parsedPlan = parser.Parse(buffer.WrittenMemory);

        // assert
        var originalSubPlanNode = originalPlan.DeferredSubPlans
            .Single()
            .AllNodes
            .OfType<OperationExecutionNode>()
            .Single(n => n.ParentDependencies.Length > 0);
        var parsedSubPlanNode = parsedPlan.DeferredSubPlans
            .Single()
            .AllNodes
            .OfType<OperationExecutionNode>()
            .Single(n => n.Id == originalSubPlanNode.Id);
        Assert.Equal(
            originalSubPlanNode.ParentDependencies.ToArray(),
            parsedSubPlanNode.ParentDependencies.ToArray());
        Assert.Equal(
            originalSubPlanNode.Dependencies.Length,
            parsedSubPlanNode.Dependencies.Length);
    }

    [Fact]
    public void Parse_Plan_Without_BatchingGroupId()
    {
        // arrange
        // Strip batchingGroupId from a formatted plan to simulate a legacy payload.
        var compositeSchema = CreateCompositeSchema();
        var originalPlan = PlanOperation(
            compositeSchema,
            """
            {
                productBySlug(slug: "1") {
                    id
                    name
                    estimatedDelivery(postCode: "12345")
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
        formatter.Format(buffer, originalPlan);

        var json = JsonNode.Parse(buffer.WrittenSpan)!;
        foreach (var node in json["nodes"]!.AsArray())
        {
            if (node?["type"]?.GetValue<string>() is "Operation")
            {
                node.AsObject().Remove("batchingGroupId");
            }
        }
        var legacyPlanSource = Encoding.UTF8.GetBytes(
            json.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        var compiler = new OperationCompiler(
            compositeSchema,
            new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
                new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>()));
        var parser = new JsonOperationPlanParser(compiler);

        // act
        var parsedPlan = parser.Parse(legacyPlanSource);

        // assert
        Assert.NotEmpty(parsedPlan.AllNodes.OfType<OperationExecutionNode>());
    }
}
