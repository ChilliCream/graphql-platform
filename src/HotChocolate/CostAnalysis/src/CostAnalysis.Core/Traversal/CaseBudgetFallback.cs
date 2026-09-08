using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// The conservative bound <see cref="ExactCasesTraversal"/> falls back to
/// once the <see cref="CaseBudget"/> is exhausted: joins each remaining
/// variable's false and true cases independently of every other remaining
/// variable, rather than splitting on all of them jointly.
/// </summary>
internal static class CaseBudgetFallback
{
    /// <summary>
    /// Evaluates the remaining pending variables of one exact case
    /// independently and joins their results.
    /// </summary>
    public static BooleanDecision<TSummary> Evaluate<TSummary>(
        CostSchemaSnapshot snapshot,
        IReadOnlyDictionary<string, FragmentDefinitionNode> fragments,
        ConditionTree tree,
        IAnalysisAlgebra<TSummary> algebra,
        CaseBudget budget,
        PossibleTypeSet region,
        int representative,
        BooleanAssignment assignment,
        List<string> pendingVariables)
    {
        BooleanDecision<TSummary>? combined = null;

        foreach (var variable in pendingVariables)
        {
            var whenFalse = EvaluateCaseEnvelope(
                snapshot, fragments, tree, algebra, budget, region, representative, assignment.With(variable, false));
            var whenTrue = EvaluateCaseEnvelope(
                snapshot, fragments, tree, algebra, budget, region, representative, assignment.With(variable, true));
            var branch = BooleanDecision<TSummary>.Split(
                variable,
                BooleanDecision<TSummary>.Leaf(whenFalse),
                BooleanDecision<TSummary>.Leaf(whenTrue));

            combined = combined is null
                ? branch
                : BooleanDecision<TSummary>.ZipWith(combined, branch, algebra.Join);
        }

        return combined!;
    }

    /// <summary>
    /// Evaluates one region's boundary in envelope mode: every Boolean edge
    /// the assignment does not resolve is followed regardless of its value,
    /// collapsing the remaining uncertainty into one summary.
    /// </summary>
    private static TSummary EvaluateCaseEnvelope<TSummary>(
        CostSchemaSnapshot snapshot,
        IReadOnlyDictionary<string, FragmentDefinitionNode> fragments,
        ConditionTree tree,
        IAnalysisAlgebra<TSummary> algebra,
        CaseBudget budget,
        PossibleTypeSet region,
        int representative,
        BooleanAssignment assignment)
    {
        var visited = new List<int>();
        var visitedSet = new HashSet<int>();
        CollectReachableWildcard(tree, representative, assignment, tree.RootNodeId, visited, visitedSet);
        return CollectAndWeighEnvelope(snapshot, fragments, algebra, budget, region, assignment, tree, visited);
    }

    /// <summary>
    /// Evaluates a boundary entirely in envelope mode, joining across its
    /// own type regions the same way the exact backend does.
    /// </summary>
    private static TSummary EvaluateBoundaryEnvelope<TSummary>(
        CostSchemaSnapshot snapshot,
        IReadOnlyDictionary<string, FragmentDefinitionNode> fragments,
        ConditionTree tree,
        IAnalysisAlgebra<TSummary> algebra,
        CaseBudget budget,
        BooleanAssignment assignment)
    {
        var regions = TypeRegionPartitioner.Partition(snapshot, tree.Root.Condition.PossibleTypes, ExactCasesTraversal.CollectTypeConditions(tree));
        TSummary? combined = default;
        var hasCombined = false;

        foreach (var region in regions)
        {
            if (region.Count == 0)
            {
                continue;
            }

            var representative = ExactCasesTraversal.FirstIndex(region);
            var value = EvaluateCaseEnvelope(snapshot, fragments, tree, algebra, budget, region, representative, assignment);
            combined = hasCombined ? algebra.Join(combined!, value) : value;
            hasCombined = true;
        }

        return hasCombined ? combined! : algebra.Empty;
    }

    private static TSummary CollectAndWeighEnvelope<TSummary>(
        CostSchemaSnapshot snapshot,
        IReadOnlyDictionary<string, FragmentDefinitionNode> fragments,
        IAnalysisAlgebra<TSummary> algebra,
        CaseBudget budget,
        PossibleTypeSet region,
        BooleanAssignment assignment,
        ConditionTree tree,
        List<int> visited)
    {
        TSummary? combined = default;
        var hasCombined = false;

        foreach (var (responseName, fields) in FieldGroupMerger.Merge(tree, visited))
        {
            var fieldName = fields[0].Name.Value;
            var members = BuildMembers(snapshot, region, fieldName);
            var childSelections = FieldGroupMerger.MergedSelections(fields);
            var childValue = childSelections.Count == 0
                ? algebra.Empty
                : EvaluateChildEnvelope(snapshot, fragments, algebra, budget, assignment, members[0].Field, childSelections);
            var groupValue = algebra.Field(new CollectedFieldGroup(responseName, members, listMultiplier: 1.0), childValue);

            combined = hasCombined ? algebra.Combine(combined!, groupValue) : groupValue;
            hasCombined = true;
        }

        return hasCombined ? combined! : algebra.Empty;
    }

    private static TSummary EvaluateChildEnvelope<TSummary>(
        CostSchemaSnapshot snapshot,
        IReadOnlyDictionary<string, FragmentDefinitionNode> fragments,
        IAnalysisAlgebra<TSummary> algebra,
        CaseBudget budget,
        BooleanAssignment assignment,
        IOutputFieldDefinition field,
        IReadOnlyList<ISelectionNode> childSelections)
    {
        var returnTypeName = field.Type.NamedType().Name;
        var childRoot = new Condition(snapshot.GetPossibleTypeSet(returnTypeName), []);
        var childTree = ConditionTreeExtractor.ExtractBoundary(snapshot, fragments, childSelections, childRoot);
        return EvaluateBoundaryEnvelope(snapshot, fragments, childTree, algebra, budget, assignment);
    }

    /// <summary>
    /// Like <see cref="ExactCasesTraversal"/>'s reachability walk, except a
    /// Boolean edge the assignment does not resolve is followed
    /// unconditionally rather than deferred.
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
                    CollectReachableWildcard(tree, representative, assignment, branch.TargetNodeId, visited, visitedSet);
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

    private static CollectedFieldGroupMember[] BuildMembers(CostSchemaSnapshot snapshot, PossibleTypeSet region, string fieldName)
    {
        var members = new CollectedFieldGroupMember[region.Count];
        var i = 0;

        foreach (var typeIndex in region)
        {
            members[i++] = new CollectedFieldGroupMember(
                snapshot.GetObjectTypeDefinition(typeIndex),
                snapshot.GetFieldDefinition(typeIndex, fieldName));
        }

        return members;
    }
}
