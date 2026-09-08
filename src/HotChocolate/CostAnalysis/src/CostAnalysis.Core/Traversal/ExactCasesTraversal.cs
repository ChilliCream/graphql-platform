using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// The ExactCases backend: walks a boundary's type regions and Boolean
/// branches to produce a <see cref="BooleanDecision{T}"/> over an analysis
/// algebra's summary, collecting fields by response name before weighing
/// them. Falls back to <see cref="CaseBudgetFallback"/> once the
/// <see cref="CaseBudget"/> is exhausted.
/// </summary>
internal static class ExactCasesTraversal
{
    /// <summary>
    /// Evaluates the operation's root boundary.
    /// </summary>
    /// <param name="snapshot">
    /// The schema snapshot to resolve types, fields and weights against.
    /// </param>
    /// <param name="fragments">
    /// The document's named fragment definitions, needed to extract nested
    /// boundaries lazily as field groups are collected.
    /// </param>
    /// <param name="tree">
    /// The root boundary's condition tree.
    /// </param>
    /// <param name="algebra">
    /// The analysis algebra to evaluate.
    /// </param>
    /// <param name="budget">
    /// The per-operation case budget, shared across every boundary this
    /// evaluation recurses into.
    /// </param>
    public static BooleanDecision<TSummary> Evaluate<TSummary>(
        CostSchemaSnapshot snapshot,
        IReadOnlyDictionary<string, FragmentDefinitionNode> fragments,
        ConditionTree tree,
        IAnalysisAlgebra<TSummary> algebra,
        CaseBudget budget)
        => EvaluateBoundary(snapshot, fragments, tree, algebra, budget, BooleanAssignment.Empty);

    /// <summary>
    /// Evaluates one boundary: joins the result of every type region in its
    /// scope, each factored over the Boolean variables the region's own
    /// branches and its fields' nested boundaries introduce.
    /// </summary>
    private static BooleanDecision<TSummary> EvaluateBoundary<TSummary>(
        CostSchemaSnapshot snapshot,
        IReadOnlyDictionary<string, FragmentDefinitionNode> fragments,
        ConditionTree tree,
        IAnalysisAlgebra<TSummary> algebra,
        CaseBudget budget,
        BooleanAssignment assignment)
    {
        var regions = TypeRegionPartitioner.Partition(snapshot, tree.Root.Condition.PossibleTypes, CollectTypeConditions(tree));
        BooleanDecision<TSummary>? combined = null;

        foreach (var region in regions)
        {
            if (region.Count == 0)
            {
                continue;
            }

            var representative = FirstIndex(region);
            var regionResult = EvaluateCase(snapshot, fragments, tree, algebra, budget, region, representative, assignment);
            combined = combined is null
                ? regionResult
                : BooleanDecision<TSummary>.ZipWith(combined, regionResult, algebra.Join);
        }

        return combined ?? BooleanDecision<TSummary>.Leaf(algebra.Empty);
    }

    /// <summary>
    /// Evaluates one type region: follows every branch the current
    /// assignment already resolves, and splits on the canonically first
    /// still-unresolved variable it finds reachable, in one canonical order
    /// shared by the whole selection hierarchy.
    /// </summary>
    private static BooleanDecision<TSummary> EvaluateCase<TSummary>(
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
        var pending = new List<BooleanLiteral>();
        CollectReachable(tree, representative, assignment, tree.RootNodeId, visited, visitedSet, pending);

        if (pending.Count == 0)
        {
            return CollectAndWeigh(snapshot, fragments, tree, algebra, budget, region, assignment, visited);
        }

        var variable = PickCanonicalVariable(pending);

        if (!budget.TrySpend())
        {
            return CaseBudgetFallback.Evaluate(
                snapshot,
                fragments,
                tree,
                algebra,
                budget,
                region,
                representative,
                assignment,
                DistinctVariables(pending));
        }

        var whenFalse = EvaluateCase(snapshot, fragments, tree, algebra, budget, region, representative, assignment.With(variable, false));
        var whenTrue = EvaluateCase(snapshot, fragments, tree, algebra, budget, region, representative, assignment.With(variable, true));
        return BooleanDecision<TSummary>.Split(variable, whenFalse, whenTrue);
    }

    /// <summary>
    /// Walks the condition tree DAG from <paramref name="nodeId"/>, following
    /// a type edge whose target still contains <paramref name="representative"/>
    /// and a Boolean edge the current assignment resolves as active. An edge
    /// whose variable is not yet assigned is recorded in
    /// <paramref name="pending"/> instead of being followed.
    /// </summary>
    private static void CollectReachable(
        ConditionTree tree,
        int representative,
        BooleanAssignment assignment,
        int nodeId,
        List<int> visited,
        HashSet<int> visitedSet,
        List<BooleanLiteral> pending)
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
                    CollectReachable(tree, representative, assignment, branch.TargetNodeId, visited, visitedSet, pending);
                }

                continue;
            }

            var literal = branch.Condition.Literal!.Value;

            if (assignment.TryGetValue(literal.VariableName, out var value))
            {
                if (value == literal.IsPositive)
                {
                    CollectReachable(tree, representative, assignment, branch.TargetNodeId, visited, visitedSet, pending);
                }

                continue;
            }

            pending.Add(literal);
        }
    }

    /// <summary>
    /// Collects every visited node's field groups by response name, then,
    /// for each group, prices it through every possible type in
    /// <paramref name="region"/> and its own nested boundary, combining
    /// every group's contribution.
    /// </summary>
    private static BooleanDecision<TSummary> CollectAndWeigh<TSummary>(
        CostSchemaSnapshot snapshot,
        IReadOnlyDictionary<string, FragmentDefinitionNode> fragments,
        ConditionTree tree,
        IAnalysisAlgebra<TSummary> algebra,
        CaseBudget budget,
        PossibleTypeSet region,
        BooleanAssignment assignment,
        List<int> visited)
    {
        BooleanDecision<TSummary>? combined = null;

        foreach (var (responseName, fields) in FieldGroupMerger.Merge(tree, visited))
        {
            var fieldName = fields[0].Name.Value;
            var members = BuildMembers(snapshot, region, fieldName);
            var childSelections = FieldGroupMerger.MergedSelections(fields);
            var childDecision = childSelections.Count == 0
                ? BooleanDecision<TSummary>.Leaf(algebra.Empty)
                : EvaluateChild(snapshot, fragments, algebra, budget, assignment, members[0].Field, childSelections);
            var groupDecision = MapField(algebra, responseName, members, childDecision);

            combined = combined is null
                ? groupDecision
                : BooleanDecision<TSummary>.ZipWith(combined, groupDecision, algebra.Combine);
        }

        return combined ?? BooleanDecision<TSummary>.Leaf(algebra.Empty);
    }

    /// <summary>
    /// Extracts and evaluates a field's nested boundary, seeded with the
    /// current case's assignment.
    /// </summary>
    private static BooleanDecision<TSummary> EvaluateChild<TSummary>(
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
        return EvaluateBoundary(snapshot, fragments, childTree, algebra, budget, assignment);
    }

    /// <summary>
    /// Maps <see cref="IAnalysisAlgebra{T}.Field"/> over every leaf of a
    /// possibly-split child decision, producing the group's own decision.
    /// </summary>
    private static BooleanDecision<TSummary> MapField<TSummary>(
        IAnalysisAlgebra<TSummary> algebra,
        string responseName,
        CollectedFieldGroupMember[] members,
        BooleanDecision<TSummary> child)
    {
        if (child is LeafDecision<TSummary> leaf)
        {
            return BooleanDecision<TSummary>.Leaf(
                algebra.Field(new CollectedFieldGroup(responseName, members, listMultiplier: 1.0), leaf.Value));
        }

        var split = (SplitDecision<TSummary>)child;
        return BooleanDecision<TSummary>.Split(
            split.Variable,
            MapField(algebra, responseName, members, split.WhenFalse),
            MapField(algebra, responseName, members, split.WhenTrue));
    }

    /// <summary>
    /// Builds one <see cref="CollectedFieldGroupMember"/> per possible type
    /// in <paramref name="region"/>, each resolving <paramref name="fieldName"/>
    /// through its own field definition.
    /// </summary>
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

    /// <summary>
    /// Gets the one representative object-type index of a non-empty region.
    /// </summary>
    internal static int FirstIndex(PossibleTypeSet set)
    {
        var enumerator = set.GetEnumerator();
        enumerator.MoveNext();
        return enumerator.Current;
    }

    /// <summary>
    /// Collects every distinct type condition appearing anywhere in
    /// <paramref name="tree"/>, the distinguishing conditions type regions
    /// are partitioned against.
    /// </summary>
    internal static List<PossibleTypeSet> CollectTypeConditions(ConditionTree tree)
    {
        var seen = new HashSet<PossibleTypeSet>();
        var result = new List<PossibleTypeSet>();

        foreach (var node in tree.Nodes)
        {
            if (seen.Add(node.Condition.PossibleTypes))
            {
                result.Add(node.Condition.PossibleTypes);
            }
        }

        return result;
    }

    /// <summary>
    /// Picks the canonically first (ordinal) variable name among a set of
    /// pending literals.
    /// </summary>
    internal static string PickCanonicalVariable(List<BooleanLiteral> pending)
    {
        var best = pending[0].VariableName;

        for (var i = 1; i < pending.Count; i++)
        {
            if (string.CompareOrdinal(pending[i].VariableName, best) < 0)
            {
                best = pending[i].VariableName;
            }
        }

        return best;
    }

    /// <summary>
    /// Gets the distinct variable names among a set of pending literals, in
    /// canonical order.
    /// </summary>
    internal static List<string> DistinctVariables(List<BooleanLiteral> pending)
    {
        var names = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var literal in pending)
        {
            names.Add(literal.VariableName);
        }

        return [.. names];
    }
}
