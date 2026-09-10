using System.Diagnostics.CodeAnalysis;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Fusion.Planning;

public sealed class GeneratedOperationNameTests : FusionTestBase
{
    [Fact]
    public void CreatePlan_Should_KeepTheShortHash_When_ItHoldsNameCharactersOnly()
    {
        // arrange
        var schema = CreateCompositeSchema();

        // act
        var plan = CreatePlan(schema, "abcdef12");

        // assert
        Assert.Equal("Op_abcdef12_1", GetOperationName(plan));
    }

    private static string GetOperationName(OperationPlan plan)
        => plan.AllNodes.OfType<OperationExecutionNode>().First().Operation.Name;

    private static OperationPlan CreatePlan(FusionSchemaDefinition schema, string shortHash)
    {
        var pool = new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
            new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>());
        var planner = new OperationPlanner(schema, new OperationCompiler(schema, pool));

        return planner.CreatePlan(
            "operation-id",
            "0123456789abcdef",
            shortHash,
            ParseOperation("{ productBySlug(slug: \"1\") { id } }"));
    }

    private static OperationDefinitionNode ParseOperation([StringSyntax("graphql")] string operationText)
        => Utf8GraphQLParser.Parse(operationText).Definitions.OfType<OperationDefinitionNode>().First();
}
