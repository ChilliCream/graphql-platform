using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Execution.Nodes.Serialization;
using HotChocolate.Fusion.Types;
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
}
