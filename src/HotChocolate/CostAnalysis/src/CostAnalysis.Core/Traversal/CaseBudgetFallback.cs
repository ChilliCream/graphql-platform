using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// Computes a conservative bound when exact analysis exceeds the case budget.
/// Adding fields or increasing child summaries must not reduce the supplied algebra's result.
/// </summary>
internal static class CaseBudgetFallback
{
    /// <summary>
    /// Returns a bound for one type region covering all unresolved Boolean alternatives.
    /// </summary>
    public static BooleanDecision<TSummary> Evaluate<TSummary>(
        CostSchemaIndex schemaIndex,
        IReadOnlyDictionary<string, FragmentDefinitionNode> fragments,
        ConditionTree tree,
        IAnalysisAlgebra<TSummary> algebra,
        ICostVariableValues? variableValues,
        CaseBudget budget,
        PossibleTypeSet region,
        int representative,
        BooleanAssignment assignment,
        ExactCasesTraversal.TraversalCache cache,
        SizedFieldContext? parentSizeContext)
        => BooleanDecision<TSummary>.Leaf(
            EvaluateCaseEnvelope(
                schemaIndex,
                fragments,
                tree,
                algebra,
                variableValues,
                budget,
                region,
                representative,
                assignment,
                cache,
                parentSizeContext));

    /// <summary>
    /// Computes a bound for one type region covering all unresolved Boolean alternatives.
    /// </summary>
    private static TSummary EvaluateCaseEnvelope<TSummary>(
        CostSchemaIndex schemaIndex,
        IReadOnlyDictionary<string, FragmentDefinitionNode> fragments,
        ConditionTree tree,
        IAnalysisAlgebra<TSummary> algebra,
        ICostVariableValues? variableValues,
        CaseBudget budget,
        PossibleTypeSet region,
        int representative,
        BooleanAssignment assignment,
        ExactCasesTraversal.TraversalCache cache,
        SizedFieldContext? parentSizeContext)
    {
        var visited = new List<int>();
        var visitedSet = new HashSet<int>();
        CollectReachableWildcard(tree, representative, assignment, tree.RootNodeId, visited, visitedSet);
        return CollectAndWeighEnvelope(
            schemaIndex,
            fragments,
            algebra,
            variableValues,
            budget,
            region,
            assignment,
            tree,
            visited,
            cache,
            parentSizeContext);
    }

    /// <summary>
    /// Computes a bound for a selection set covering all possible types and Boolean alternatives.
    /// </summary>
    private static TSummary EvaluateBoundaryEnvelope<TSummary>(
        CostSchemaIndex schemaIndex,
        IReadOnlyDictionary<string, FragmentDefinitionNode> fragments,
        ConditionTree tree,
        IAnalysisAlgebra<TSummary> algebra,
        ICostVariableValues? variableValues,
        CaseBudget budget,
        BooleanAssignment assignment,
        ExactCasesTraversal.TraversalCache cache,
        SizedFieldContext? parentSizeContext)
    {
        var regions = TypeRegionPartitioner.Partition(
            schemaIndex,
            tree.Root.Condition.PossibleTypes,
            ExactCasesTraversal.CollectTypeConditions(tree));
        TSummary? combined = default;
        var hasCombined = false;

        foreach (var region in regions)
        {
            if (region.Count == 0)
            {
                continue;
            }

            var representative = ExactCasesTraversal.FirstIndex(region);
            var value = EvaluateCaseEnvelope(
                schemaIndex,
                fragments,
                tree,
                algebra,
                variableValues,
                budget,
                region,
                representative,
                assignment,
                cache,
                parentSizeContext);
            combined = hasCombined ? algebra.Join(combined!, value) : value;
            hasCombined = true;
        }

        return hasCombined ? combined! : algebra.Empty;
    }

    private static TSummary CollectAndWeighEnvelope<TSummary>(
        CostSchemaIndex schemaIndex,
        IReadOnlyDictionary<string, FragmentDefinitionNode> fragments,
        IAnalysisAlgebra<TSummary> algebra,
        ICostVariableValues? variableValues,
        CaseBudget budget,
        PossibleTypeSet region,
        BooleanAssignment assignment,
        ConditionTree tree,
        List<int> visited,
        ExactCasesTraversal.TraversalCache cache,
        SizedFieldContext? parentSizeContext)
    {
        TSummary? combined = default;
        var hasCombined = false;

        foreach (var (responseName, fields) in FieldGroupMerger.Merge(tree, visited))
        {
            var fieldName = fields[0].Name.Value;
            var members = cache.GetMembers(region, fieldName);

            if (members.Length == 0)
            {
                continue;
            }

            var childSelections = FieldGroupMerger.MergedSelections(fields);
            TSummary? groupValue = default;
            var hasGroupValue = false;

            foreach (var field in fields)
            {
                foreach (var member in members)
                {
                    var childSizeContext = InheritedListSizes.Resolve(schemaIndex, member, field.Arguments);
                    var childValue = childSelections.Count == 0
                        ? algebra.Empty
                        : EvaluateChildEnvelope(
                            schemaIndex,
                            fragments,
                            algebra,
                            variableValues,
                            budget,
                            assignment,
                            member,
                            fields,
                            childSelections,
                            cache,
                            childSizeContext);
                    var group = new CollectedFieldGroup(
                        responseName,
                        field,
                        member,
                        InheritedListSizes.InheritedSizeFor(parentSizeContext, fieldName, variableValues));
                    var pairValue = algebra is IInheritedSizePlanAlgebra<TSummary> planAlgebra
                        ? planAlgebra.Field(group, parentSizeContext, childValue)
                        : algebra.Field(group, childValue);

                    groupValue = hasGroupValue ? algebra.Join(groupValue!, pairValue) : pairValue;
                    hasGroupValue = true;
                }
            }

            combined = hasCombined ? algebra.Combine(combined!, groupValue!) : groupValue;
            hasCombined = true;
        }

        return hasCombined ? combined! : algebra.Empty;
    }

    /// <summary>
    /// Computes a bound for a field selection's children under one possible parent type.
    /// </summary>
    private static TSummary EvaluateChildEnvelope<TSummary>(
        CostSchemaIndex schemaIndex,
        IReadOnlyDictionary<string, FragmentDefinitionNode> fragments,
        IAnalysisAlgebra<TSummary> algebra,
        ICostVariableValues? variableValues,
        CaseBudget budget,
        BooleanAssignment assignment,
        CollectedFieldGroupMember member,
        IReadOnlyList<FieldNode> fields,
        IReadOnlyList<ISelectionNode> childSelections,
        ExactCasesTraversal.TraversalCache cache,
        SizedFieldContext? parentSizeContext)
    {
        var returnTypeName = member.Field.Type.NamedType().Name;
        var childTree = cache.GetChildBoundary(returnTypeName, fields, childSelections);
        return EvaluateBoundaryEnvelope(
            schemaIndex,
            fragments,
            childTree,
            algebra,
            variableValues,
            budget,
            assignment,
            cache,
            parentSizeContext);
    }

    /// <summary>
    /// Collects fields reachable under the assignment, including all unresolved Boolean alternatives.
    /// </summary>
    private static void CollectReachableWildcard(
        ConditionTree tree,
        int representative,
        BooleanAssignment assignment,
        int nodeId,
        List<int> visited,
        HashSet<int> visitedSet)
    {
        if (!visitedSet.Add(nodeId))
        {
            return;
        }

        visited.Add(nodeId);

        foreach (var branch in tree.Nodes[nodeId].Branches)
        {
            if (branch.Condition.TypeName is not null)
            {
                if (tree.Nodes[branch.TargetNodeId].Condition.PossibleTypes.Contains(representative))
                {
                    CollectReachableWildcard(
                        tree,
                        representative,
                        assignment,
                        branch.TargetNodeId,
                        visited,
                        visitedSet);
                }

                continue;
            }

            var literal = branch.Condition.Literal!.Value;

            if (assignment.TryGetValue(literal.VariableName, out var value) && value != literal.IsPositive)
            {
                continue;
            }

            CollectReachableWildcard(tree, representative, assignment, branch.TargetNodeId, visited, visitedSet);
        }
    }
}
