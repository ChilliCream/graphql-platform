using System.Collections.Immutable;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Planning.Partitioners;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

public sealed partial class OperationPlanner
{
    private (PlanNode Node, SelectionSet First) CreateQueryPlanBase(
        OperationDefinitionNode operationDefinition,
        string shortHash,
        ISelectionSetIndex index)
    {
        var selectionSet = new SelectionSet(
            index.GetId(operationDefinition.SelectionSet),
            operationDefinition.SelectionSet,
            _schema.GetOperationType(operationDefinition.Operation),
            SelectionPath.Root);

        var input = new RootSelectionSetPartitionerInput { SelectionSet = selectionSet, SelectionSetIndex = index };
        var result = _nodeFieldSelectionSetPartitioner.Partition(input);

        var backlog = Backlog.Empty;

        if (result.SelectionSet is not null)
        {
            backlog = backlog.Push(OperationWorkItem.CreateRoot(result.SelectionSet));
        }

        if (result.NodeFields is not null)
        {
            foreach (var nodeField in result.NodeFields)
            {
                backlog = backlog.Push(new NodeFieldWorkItem(nodeField));
            }
        }

        var remainingCost =
            PlannerCostEstimator.EstimateRemainingCost(
                _options,
                currentMaxDepth: 0,
                ImmutableDictionary<int, int>.Empty,
                backlog.Cost);

        var node = new PlanNode
        {
            OperationDefinition = operationDefinition,
            InternalOperationDefinition = operationDefinition,
            ShortHash = shortHash,
            SchemaName = Planning.PlanNode.UnresolvedSchemaName,
            Options = _options,
            SelectionSetIndex = result.SelectionSetIndex,
            Backlog = backlog,
            RemainingCost = remainingCost,
            OperationStepCount = 0
        };

        return (node, selectionSet);
    }

    private (PlanNode Node, SelectionSet First) CreateMutationPlanBase(
        OperationDefinitionNode operationDefinition,
        string shortHash,
        ISelectionSetIndex index)
    {
        // For mutations, we slice the root selection set into individual root selections,
        // so that we can plan each root selection separately. This aligns with the
        // GraphQL mutation execution algorithm where mutation fields at the root level
        // must be executed sequentially: execute the first mutation field and all its
        // child selections (which represent the query of the mutation's affected state),
        // then move to the next mutation field and repeat.
        //
        // The plan will end up with separate root nodes for each mutation field, and the
        // plan executor will execute these root nodes in document order.
        var backlog = Backlog.Empty;
        var selectionSetId = index.GetId(operationDefinition.SelectionSet);
        var indexBuilder = index.ToBuilder();
        SelectionSet firstSelectionSet = null!;

        // We traverse in reverse order and push to the stack so that the first mutation
        // field (index 0) will end up on top of the stack and be processed first.
        // Due to LIFO stack behavior, the last selection we push becomes the first processed.
        for (var i = operationDefinition.SelectionSet.Selections.Count - 1; i >= 0; i--)
        {
            var rootSelection = operationDefinition.SelectionSet.Selections[i];
            var rootSelectionSet = new SelectionSetNode([rootSelection]);
            indexBuilder.Register(selectionSetId, rootSelectionSet);

            var selectionSet = new SelectionSet(
                selectionSetId,
                rootSelectionSet,
                _schema.GetOperationType(operationDefinition.Operation),
                SelectionPath.Root);

            // firstSelectionSet gets overwritten each iteration and ends up holding
            // the selection from the last loop iteration (i=0), which corresponds to
            // the first mutation field in document order and the first to be processed.
            firstSelectionSet = selectionSet;
            backlog = backlog.Push(OperationWorkItem.CreateRoot(selectionSet));
        }

        var remainingCost =
            PlannerCostEstimator.EstimateRemainingCost(
                _options,
                currentMaxDepth: 0,
                ImmutableDictionary<int, int>.Empty,
                backlog.Cost);

        var node = new PlanNode
        {
            OperationDefinition = operationDefinition,
            InternalOperationDefinition = operationDefinition,
            ShortHash = shortHash,
            SchemaName = ISchemaDefinition.DefaultName,
            Options = _options,
            SelectionSetIndex = indexBuilder,
            Backlog = backlog,
            RemainingCost = remainingCost,
            OperationStepCount = 0
        };

        return (node, firstSelectionSet);
    }

    private (PlanNode Node, SelectionSet First) CreateSubscriptionPlanBase(
        OperationDefinitionNode operationDefinition,
        string shortHash,
        ISelectionSetIndex index)
    {
        var selectionSet = new SelectionSet(
            index.GetId(operationDefinition.SelectionSet),
            operationDefinition.SelectionSet,
            _schema.GetOperationType(operationDefinition.Operation),
            SelectionPath.Root);

        var backlog = Backlog.Empty.Push(OperationWorkItem.CreateRoot(selectionSet));
        var remainingCost =
            PlannerCostEstimator.EstimateRemainingCost(
                _options,
                currentMaxDepth: 0,
                ImmutableDictionary<int, int>.Empty,
                backlog.Cost);

        var node = new PlanNode
        {
            OperationDefinition = operationDefinition,
            InternalOperationDefinition = operationDefinition,
            ShortHash = shortHash,
            SchemaName = Planning.PlanNode.UnresolvedSchemaName,
            Options = _options,
            SelectionSetIndex = index,
            Backlog = backlog,
            RemainingCost = remainingCost,
            OperationStepCount = 0
        };

        return (node, selectionSet);
    }
}
