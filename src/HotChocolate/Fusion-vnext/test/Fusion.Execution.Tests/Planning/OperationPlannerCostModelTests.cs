using System.Collections.Immutable;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Language;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Fusion.Planning;

public class OperationPlannerCostModelTests : FusionTestBase
{
    [Fact]
    public void PathCost_Defaults_Prefer_ModerateFanout_To_SequentialChain()
    {
        // 4 sequential operations.
        var sequential = CreateNode(maxDepth: 4, operationStepCount: 4, excessFanout: 0);

        // 1 root + 6 parallel lookups.
        var moderateFanout = CreateNode(maxDepth: 2, operationStepCount: 7, excessFanout: 0);

        Assert.True(moderateFanout.PathCost < sequential.PathCost);
        Assert.Equal(66.0, sequential.PathCost, 6);
        Assert.Equal(40.5, moderateFanout.PathCost, 6);
    }

    [Fact]
    public void PathCost_Defaults_Penalize_ExcessiveFanout()
    {
        // 1 root + 8 parallel lookups (at threshold).
        var moderateFanout = CreateNode(maxDepth: 2, operationStepCount: 9, excessFanout: 0);

        // 1 root + 20 parallel lookups.
        var excessiveFanout = CreateNode(maxDepth: 2, operationStepCount: 21, excessFanout: 12);

        Assert.True(moderateFanout.PathCost < excessiveFanout.PathCost);
        Assert.Equal(43.5, moderateFanout.PathCost, 6);
        Assert.Equal(97.5, excessiveFanout.PathCost, 6);
    }

    [Fact]
    public void Constructors_Wire_Default_And_Custom_Options()
    {
        var schema = CreateCompositeSchema();
        var pool = new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
            new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>());
        var compiler = new OperationCompiler(schema, pool);

        var defaultPlanner = new OperationPlanner(schema, compiler);

        Assert.Equal(15.0, defaultPlanner.Options.DepthWeight);
        Assert.Equal(1.5, defaultPlanner.Options.OperationWeight);
        Assert.Equal(3.0, defaultPlanner.Options.ExcessFanoutWeight);
        Assert.Equal(8, defaultPlanner.Options.FanoutPenaltyThreshold);

        var customOptions = new OperationPlannerOptions
        {
            DepthWeight = 9.0,
            OperationWeight = 5.0,
            ExcessFanoutWeight = 2.0,
            FanoutPenaltyThreshold = 4
        };

        var customPlanner = new OperationPlanner(schema, compiler, customOptions);

        Assert.Equal(customOptions.DepthWeight, customPlanner.Options.DepthWeight);
        Assert.Equal(customOptions.OperationWeight, customPlanner.Options.OperationWeight);
        Assert.Equal(customOptions.ExcessFanoutWeight, customPlanner.Options.ExcessFanoutWeight);
        Assert.Equal(customOptions.FanoutPenaltyThreshold, customPlanner.Options.FanoutPenaltyThreshold);
    }

    private static PlanNode CreateNode(
        int maxDepth,
        int operationStepCount,
        int excessFanout,
        OperationPlannerOptions? options = null)
    {
        var operationDefinition = Utf8GraphQLParser
            .Parse("query Test { __typename }")
            .Definitions
            .OfType<OperationDefinitionNode>()
            .Single();

        return new PlanNode
        {
            OperationDefinition = operationDefinition,
            InternalOperationDefinition = operationDefinition,
            ShortHash = "abcdef12",
            SchemaName = "None",
            Options = options ?? OperationPlannerOptions.Default,
            SelectionSetIndex = SelectionSetIndexer.Create(operationDefinition),
            Backlog = ImmutableStack<WorkItem>.Empty,
            BacklogLowerBound = 0,
            OperationStepCount = operationStepCount,
            MaxDepth = maxDepth,
            ExcessFanout = excessFanout
        };
    }
}
