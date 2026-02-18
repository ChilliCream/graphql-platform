using System.Diagnostics.CodeAnalysis;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Fusion.Planning;

public sealed class OperationPlannerGuardrailTests : FusionTestBase
{
    [Fact]
    public void CreatePlan_Throws_When_MaxExpandedNodes_Guardrail_Is_Exceeded()
    {
        // arrange
        var schema = CreateCompositeSchema();
        var planner = CreatePlanner(
            schema,
            new OperationPlannerOptions
            {
                MaxExpandedNodes = 1
            });
        var operation = ParseOperation(TestOperationText);

        // act
        var error = Assert.Throws<OperationPlannerGuardrailException>(
            () => planner.CreatePlan("guardrail-expanded", "hash", "hash", operation));

        // assert
        Assert.Equal(OperationPlannerGuardrailReason.MaxExpandedNodesExceeded, error.Reason);
        Assert.Equal(1, error.Limit);
        Assert.True(error.Observed > error.Limit);
    }

    [Fact]
    public void CreatePlan_Throws_When_MaxQueueSize_Guardrail_Is_Exceeded()
    {
        // arrange
        var schema = CreateCompositeSchema();
        var planner = CreatePlanner(
            schema,
            new OperationPlannerOptions
            {
                MaxQueueSize = 1
            });
        var operation = ParseOperation(TestOperationText);

        // act
        var error = Assert.Throws<OperationPlannerGuardrailException>(
            () => planner.CreatePlan("guardrail-queue", "hash", "hash", operation));

        // assert
        Assert.Equal(OperationPlannerGuardrailReason.MaxQueueSizeExceeded, error.Reason);
        Assert.Equal(1, error.Limit);
        Assert.True(error.Observed > error.Limit);
    }

    [Fact]
    public void CreatePlan_Throws_When_MaxPlanningTime_Guardrail_Is_Exceeded()
    {
        // arrange
        var schema = CreateCompositeSchema();
        var planner = CreatePlanner(
            schema,
            new OperationPlannerOptions
            {
                MaxPlanningTime = TimeSpan.FromTicks(1)
            });
        var operation = ParseOperation(TestOperationText);

        // act
        var error = Assert.Throws<OperationPlannerGuardrailException>(
            () => planner.CreatePlan("guardrail-time", "hash", "hash", operation));

        // assert
        Assert.Equal(OperationPlannerGuardrailReason.MaxPlanningTimeExceeded, error.Reason);
        Assert.True(error.Observed >= error.Limit);
    }

    [Fact]
    public void CreatePlan_Throws_When_MaxGeneratedOptions_Guardrail_Is_Exceeded()
    {
        // arrange
        var schema = CreateCompositeSchema();
        var planner = CreatePlanner(
            schema,
            new OperationPlannerOptions
            {
                MaxGeneratedOptionsPerWorkItem = 1
            });
        var operation = ParseOperation(TestOperationText);

        // act
        var error = Assert.Throws<OperationPlannerGuardrailException>(
            () => planner.CreatePlan("guardrail-generated", "hash", "hash", operation));

        // assert
        Assert.Equal(
            OperationPlannerGuardrailReason.MaxGeneratedOptionsPerWorkItemExceeded,
            error.Reason);
        Assert.Equal(1, error.Limit);
        Assert.True(error.Observed > error.Limit);
    }

    private static OperationPlanner CreatePlanner(
        FusionSchemaDefinition schema,
        OperationPlannerOptions options)
    {
        var pool = new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
            new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>());
        var compiler = new OperationCompiler(schema, pool);

        return new OperationPlanner(schema, compiler, options);
    }

    private static OperationDefinitionNode ParseOperation([StringSyntax("graphql")] string operationText)
        => Utf8GraphQLParser.Parse(operationText).Definitions.OfType<OperationDefinitionNode>().First();

    private const string TestOperationText =
        """
        {
          productBySlug(slug: "1") {
            id
            name
            estimatedDelivery(postCode: "12345")
          }
        }
        """;
}
