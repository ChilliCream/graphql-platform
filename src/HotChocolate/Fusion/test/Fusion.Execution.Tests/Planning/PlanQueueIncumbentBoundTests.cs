using System.Collections.Immutable;
using HotChocolate.Execution;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

public class PlanQueueIncumbentBoundTests : FusionTestBase
{
    // Path cost of the child below: 1 * 15 (depth) + 1 * 10 (operations) = 25.
    private static readonly OperationPlannerOptions s_options = new() { OperationWeight = 10.0 };

    private static readonly ImmutableDictionary<int, int> s_noOpsPerLevel =
#if NET10_0_OR_GREATER
        [];
#else
        ImmutableDictionary<int, int>.Empty;
#endif

    [Fact]
    public void EstimateLowestBranchRemainingCost_Should_DropDepthPenalty_When_NextLookupHasNoLookupChosen()
    {
        // arrange
        var backlog = CreateBacklog(OperationWorkItemKind.Lookup, parentDepth: 2);

        // act
        var childCost = EstimateRemainingCost(backlog);
        var lowestCost = PlanQueue.EstimateLowestBranchRemainingCost(
            s_options,
            maxDepth: 1,
            s_noOpsPerLevel,
            backlog);

        // assert
        Assert.Equal([40.0, 10.0], [childCost, lowestCost]);
    }

    [Fact]
    public void EstimateLowestBranchRemainingCost_Should_MatchChildCost_When_NextWorkItemIsRoot()
    {
        // arrange
        var backlog = CreateBacklog(OperationWorkItemKind.Root, parentDepth: 2);

        // act
        var childCost = EstimateRemainingCost(backlog);
        var lowestCost = PlanQueue.EstimateLowestBranchRemainingCost(
            s_options,
            maxDepth: 1,
            s_noOpsPerLevel,
            backlog);

        // assert
        Assert.Equal([10.0, 10.0], [childCost, lowestCost]);
    }

    [Fact]
    public void CannotBeatIncumbent_Should_ReturnFalse_When_NoIncumbentExists()
    {
        // arrange
        var backlog = CreateBacklog(OperationWorkItemKind.Root, parentDepth: 0);

        // act
        var result = CannotBeatIncumbent(backlog, double.PositiveInfinity);

        // assert
        Assert.False(result);
    }

    [Fact]
    public void CannotBeatIncumbent_Should_ReturnTrue_When_CompleteChildCostsMoreThanIncumbent()
    {
        // arrange
        var backlog = Backlog.Empty;
        const double incumbentCost = 24.9;

        // act
        var result = CannotBeatIncumbent(backlog, incumbentCost);

        // assert
        Assert.True(result);
    }

    [Fact]
    public void CannotBeatIncumbent_Should_ReturnFalse_When_CompleteChildCostsTheSameAsIncumbent()
    {
        // arrange
        var backlog = Backlog.Empty;
        const double incumbentCost = 25.0;

        // act
        var result = CannotBeatIncumbent(backlog, incumbentCost);

        // assert
        Assert.False(result);
    }

    [Fact]
    public void CannotBeatIncumbent_Should_ReturnTrue_When_RootBranchCostsMoreThanIncumbent()
    {
        // arrange
        var backlog = CreateBacklog(OperationWorkItemKind.Root, parentDepth: 2);

        // act
        var result = CannotBeatIncumbent(backlog, 34.9);

        // assert
        Assert.True(result);
    }

    [Fact]
    public void CannotBeatIncumbent_Should_ReturnFalse_When_RootBranchCostsTheSameAsIncumbent()
    {
        // arrange
        var backlog = CreateBacklog(OperationWorkItemKind.Root, parentDepth: 2);

        // act
        var result = CannotBeatIncumbent(backlog, 35.0);

        // assert
        Assert.False(result);
    }

    [Fact]
    public void CannotBeatIncumbent_Should_ReturnFalse_When_LookupBranchCanMoveToTheRoot()
    {
        // arrange
        // the child costs 25 + 10 + 2 * 15 = 65, its lookup branch can restart at the root for 35
        var backlog = CreateBacklog(OperationWorkItemKind.Lookup, parentDepth: 2);

        // act
        var result = CannotBeatIncumbent(backlog, 40.0);

        // assert
        Assert.False(result);
    }

    [Fact]
    public void CannotBeatIncumbent_Should_ReturnTrue_When_LookupBranchCostsMoreThanIncumbent()
    {
        // arrange
        var backlog = CreateBacklog(OperationWorkItemKind.Lookup, parentDepth: 2);

        // act
        var result = CannotBeatIncumbent(backlog, 34.9);

        // assert
        Assert.True(result);
    }

    private static double EstimateRemainingCost(Backlog backlog)
        => PlannerCostEstimator.EstimateRemainingCost(
            s_options,
            currentMaxDepth: 1,
            s_noOpsPerLevel,
            backlog.Cost);

    private static bool CannotBeatIncumbent(Backlog backlog, double incumbentCost)
        => PlanQueue.CannotBeatIncumbent(
            s_options,
            maxDepth: 1,
            operationStepCount: 1,
            excessFanout: 0,
            s_noOpsPerLevel,
            backlog,
            incumbentCost);

    private Backlog CreateBacklog(OperationWorkItemKind kind, int parentDepth)
    {
        var schema = CreateCompositeSchema();
        var operationDefinition = Utf8GraphQLParser
            .Parse("query Test { __typename }")
            .Definitions
            .OfType<OperationDefinitionNode>()
            .Single();

        var selectionSet = new SelectionSet(
            1,
            operationDefinition.SelectionSet,
            schema.QueryType,
            SelectionPath.Root);

        var workItem = new OperationWorkItem(kind, selectionSet, FromSchema: "test")
        {
            ParentDepth = parentDepth
        };

        return Backlog.Empty.Push(workItem);
    }
}
