using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// The conservative bound <see cref="ExactCasesTraversal"/> falls back to
/// once the <see cref="CaseBudget"/> is exhausted: every Boolean edge still
/// unresolved is followed unconditionally into one envelope summary.
/// Sound for algebras whose <see cref="IAnalysisAlgebra{T}.Field"/> is
/// monotone in the collected field set and the child summary, a property
/// both built-in algebras (<c>CostAlgebra</c>, <c>ResponseSizeAlgebra</c>)
/// satisfy.
/// </summary>
internal static class CaseBudgetFallback
{
    /// <summary>
    /// Evaluates one region's boundary in envelope mode and returns it as a
    /// resolved leaf, leaving every still-pending Boolean variable
    /// unresolved in the envelope rather than split on.
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
        SizedFieldContext? parentSizeContext)
        => BooleanDecision<TSummary>.Leaf(
            EvaluateCaseEnvelope(snapshot, fragments, tree, algebra, budget, region, representative, assignment, parentSizeContext));

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
        BooleanAssignment assignment,
        SizedFieldContext? parentSizeContext)
    {
        var visited = new List<int>();
        var visitedSet = new HashSet<int>();
        CollectReachableWildcard(tree, representative, assignment, tree.RootNodeId, visited, visitedSet);
        return CollectAndWeighEnvelope(snapshot, fragments, algebra, budget, region, assignment, tree, visited, parentSizeContext);
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
        BooleanAssignment assignment,
        SizedFieldContext? parentSizeContext)
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
            var value = EvaluateCaseEnvelope(snapshot, fragments, tree, algebra, budget, region, representative, assignment, parentSizeContext);
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
        List<int> visited,
        SizedFieldContext? parentSizeContext)
    {
        TSummary? combined = default;
        var hasCombined = false;

        foreach (var (responseName, fields) in FieldGroupMerger.Merge(tree, visited))
        {
            var fieldName = fields[0].Name.Value;
            var members = TraversalMembers.Build(snapshot, region, fieldName);

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
                    var childSizeContext = InheritedListSizes.Resolve(snapshot, member, field.Arguments);
                    var childValue = childSelections.Count == 0
                        ? algebra.Empty
                        : EvaluateChildEnvelope(snapshot, fragments, algebra, budget, assignment, member, childSelections, childSizeContext);
                    var group = new CollectedFieldGroup(
                        responseName,
                        field,
                        member,
                        InheritedListSizes.InheritedSizeFor(parentSizeContext, fieldName));
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
    /// Evaluates one child boundary for a field occurrence and parent-type
    /// pair in envelope mode.
    /// </summary>
    private static TSummary EvaluateChildEnvelope<TSummary>(
        CostSchemaSnapshot snapshot,
        IReadOnlyDictionary<string, FragmentDefinitionNode> fragments,
        IAnalysisAlgebra<TSummary> algebra,
        CaseBudget budget,
        BooleanAssignment assignment,
        CollectedFieldGroupMember member,
        IReadOnlyList<ISelectionNode> childSelections,
        SizedFieldContext? parentSizeContext)
    {
        var returnTypeName = member.Field.Type.NamedType().Name;
        var childRoot = new Condition(snapshot.GetPossibleTypeSet(returnTypeName), []);
        var childTree = ConditionTreeExtractor.ExtractBoundary(snapshot, fragments, childSelections, childRoot);
        return EvaluateBoundaryEnvelope(snapshot, fragments, childTree, algebra, budget, assignment, parentSizeContext);
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
}
