using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;

namespace HotChocolate.Fusion.Caching;

/// <summary>
/// Collects the selections that the lookups of an operation plan bring in.
/// </summary>
internal static class LookupSelectionCollector
{
    /// <summary>
    /// Gets the selections at the first level of the selection of every fetch in the plan
    /// whose results are placed below the root.
    /// </summary>
    public static IReadOnlySet<ISelection> Collect(OperationPlan operationPlan)
    {
        var selections = new HashSet<ISelection>(ReferenceEqualityComparer.Instance);
        var operation = operationPlan.Operation;

        foreach (var node in operationPlan.AllNodes)
        {
            switch (node)
            {
                case OperationExecutionNode operationNode:
                    AddTargetSelections(
                        operationNode.Target,
                        operation,
                        operationNode.ResultSelectionSet,
                        selections);
                    break;

                case ApolloOperationExecutionNode apolloNode:
                    AddTargetSelections(
                        apolloNode.Target,
                        operation,
                        apolloNode.ResultSelectionSet,
                        selections);
                    break;

                case OperationBatchExecutionNode batchNode:
                    AddDefinitionSelections(batchNode.Operations, operation, selections);
                    break;

                case ApolloOperationBatchExecutionNode apolloBatchNode:
                    AddDefinitionSelections(apolloBatchNode.Operations, operation, selections);
                    break;
            }
        }

        return selections;
    }

    private static void AddTargetSelections(
        SelectionPath target,
        Operation operation,
        ResultSelectionSet resultSelectionSet,
        HashSet<ISelection> selections)
    {
        if (target.IsRoot)
        {
            return;
        }

        foreach (var selectionSet in GetTargetSelectionSets(operation, target))
        {
            AddSelections(selectionSet, resultSelectionSet, selections);
        }
    }

    private static void AddDefinitionSelections(
        ReadOnlySpan<OperationDefinition> definitions,
        Operation operation,
        HashSet<ISelection> selections)
    {
        foreach (var definition in definitions)
        {
            switch (definition)
            {
                case SingleOperationDefinition single:
                    AddTargetSelections(
                        single.Target,
                        operation,
                        single.ResultSelectionSet,
                        selections);
                    break;

                case BatchOperationDefinition batch:
                    foreach (var target in batch.Targets)
                    {
                        AddTargetSelections(
                            target,
                            operation,
                            batch.ResultSelectionSet,
                            selections);
                    }

                    break;
            }
        }
    }

    private static List<SelectionSet> GetTargetSelectionSets(
        Operation operation,
        SelectionPath target)
    {
        List<SelectionSet> current = [operation.RootSelectionSet];

        for (var i = 0; i < target.Length; i++)
        {
            var segment = target[i];
            List<SelectionSet> next = [];

            foreach (var selectionSet in current)
            {
                switch (segment.Kind)
                {
                    case SelectionPathSegmentKind.Root:
                        next.Add(selectionSet);
                        break;

                    case SelectionPathSegmentKind.Field
                        when selectionSet.TryGetSelection(segment.Name, out var selection)
                            && !selection.IsLeaf:
                        foreach (var type in operation.GetPossibleTypes(selection))
                        {
                            next.Add(operation.GetSelectionSet(selection, type));
                        }

                        break;

                    case SelectionPathSegmentKind.InlineFragment
                        when operation.Schema.Types.TryGetType(segment.Name, out var fragmentType)
                            && fragmentType.IsAssignableFrom(selectionSet.Type):
                        next.Add(selectionSet);
                        break;
                }
            }

            current = next;
        }

        return current;
    }

    private static void AddSelections(
        SelectionSet selectionSet,
        ResultSelectionSet resultSelectionSet,
        HashSet<ISelection> selections)
    {
        foreach (var resultSelection in resultSelectionSet.Selections)
        {
            if (selectionSet.TryGetSelection(resultSelection.ResponseName, out var selection)
                && !selection.IsInternal)
            {
                selections.Add(selection);
            }
        }

        foreach (var fragment in resultSelectionSet.Fragments)
        {
            if (fragment.TypeCondition?.IsAssignableFrom(selectionSet.Type) != false)
            {
                AddSelections(selectionSet, fragment.Body, selections);
            }
        }
    }
}
