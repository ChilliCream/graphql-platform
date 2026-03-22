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

        var json = JsonNode.Parse(buffer.WrittenSpan)!;
        var operationNodes = json["nodes"]!
            .AsArray()
            .Select(t => t!.AsObject())
            .Where(t =>
            {
                var type = t["type"]?.GetValue<string>();
                return type is "Operation" or "OperationBatch";
            })
            .ToList();

        Assert.NotEmpty(operationNodes);
        Assert.All(
            operationNodes,
            node =>
            {
                Assert.True(node.ContainsKey("resultSelectionSet"));
                Assert.False(node.ContainsKey("responseNames"));
            });

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
    public void Parse_Plan_Uses_SelectionSet_Syntax_When_Present()
    {
        // arrange
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

        var planSource = Encoding.UTF8.GetBytes(
            json.ToJsonString(
                new JsonSerializerOptions
                {
                    WriteIndented = true
                }));

        var compiler = new OperationCompiler(
            compositeSchema,
            new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
                new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>()));
        var parser = new JsonOperationPlanParser(compiler);

        // act
        var parsedPlan = parser.Parse(planSource);
        var parsedOperationNode = parsedPlan.AllNodes
            .OfType<OperationExecutionNode>()
            .Single(t => t.Id == operationNodeId);

        // assert
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
    public void Parse_Plan_Without_BatchingGroupId()
    {
        // arrange
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
        var nodes = json["nodes"]!.AsArray();

        foreach (var node in nodes)
        {
            if (node?["type"]?.GetValue<string>() is "Operation")
            {
                node.AsObject().Remove("batchingGroupId");
            }
        }

        var legacyPlanSource = Encoding.UTF8.GetBytes(
            json.ToJsonString(
                new JsonSerializerOptions
                {
                    WriteIndented = true
                }));

        // act
        var compiler = new OperationCompiler(
            compositeSchema,
            new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
                new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>()));
        var parser = new JsonOperationPlanParser(compiler);
        var parsedPlan = parser.Parse(legacyPlanSource);

        // assert
        Assert.All(
            parsedPlan.AllNodes.OfType<OperationExecutionNode>(),
            node => Assert.Null(node.BatchingGroupId));
    }
}
