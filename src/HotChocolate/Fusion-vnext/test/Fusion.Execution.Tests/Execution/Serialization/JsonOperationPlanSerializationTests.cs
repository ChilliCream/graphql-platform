using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
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
        var parsedPlanFormatted = formatter.Format(parsedPlan);
        parsedPlanFormatted.MatchInlineSnapshot(Encoding.UTF8.GetString(buffer.WrittenSpan));
    }

    [Fact(Skip = "Doesn't yet work")]
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
}
