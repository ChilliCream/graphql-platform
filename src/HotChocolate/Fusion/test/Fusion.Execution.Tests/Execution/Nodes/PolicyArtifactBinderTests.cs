using System.Collections.Immutable;
using HotChocolate.Execution;
using HotChocolate.Fusion.Language;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Nodes;

public sealed class PolicyArtifactBinderTests
{
    [Fact]
    public void GetRequirements_Should_ConcatenateMemberRequirements_When_Batch()
    {
        // arrange
        // Nested-defer requirement routing can produce this batch shape. repo-n4r must pin that planner route when it lands.
        var first = CreateRequirement("first");
        var second = CreateRequirement("second");
        var third = CreateRequirement("third");
        var batch = new OperationBatchExecutionNode(
            1,
            [
                CreateOperation(2, [first, second]),
                CreateOperation(3, [third])
            ]);

        // act
        var requirements = PolicyArtifactBinder.GetBatchRequirements(batch.Operations.ToArray());

        // assert
        Assert.Equal([first, second, third], requirements);
    }

    private static OperationRequirement CreateRequirement(string map)
        => new(
            "requirement",
            Utf8GraphQLParser.Syntax.ParseTypeReference("String"),
            SelectionPath.Parse("$.product"),
            new FieldSelectionMapParser(map).Parse());

    private static SingleOperationDefinition CreateOperation(
        int id,
        OperationRequirement[] requirements)
    {
        var source = "query { product { id } }"u8.ToArray();

        return new SingleOperationDefinition(
            id,
            new OperationSourceText(
                $"Operation_{id}",
                OperationType.Query,
                source,
                OperationSourceTextHash.Compute(source)),
            lookupTypeName: null,
            schemaName: "a",
            SelectionPath.Parse("$.product"),
            SelectionPath.Parse("$.product"),
            requirements,
            forwardedVariables: [],
            ResultSelectionSet.CreateFromPlan(
                Utf8GraphQLParser.Syntax.ParseSelectionSet("{ id }")),
            conditions: [],
            requiresFileUpload: false);
    }
}
