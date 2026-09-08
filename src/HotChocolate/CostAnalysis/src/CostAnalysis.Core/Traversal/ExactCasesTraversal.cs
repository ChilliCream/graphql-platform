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
    /// <remarks>
    /// Applies <see cref="IAnalysisAlgebra{T}.Root"/> exactly once, mapped
    /// over every leaf of the root selection's decision, after the root
    /// boundary has been fully combined and joined.
    /// </remarks>
    public static BooleanDecision<TSummary> Evaluate<TSummary>(
        CostSchemaSnapshot snapshot,
        IReadOnlyDictionary<string, FragmentDefinitionNode> fragments,
        ConditionTree tree,
        IAnalysisAlgebra<TSummary> algebra,
        CaseBudget budget)
    {
        var selection = EvaluateBoundary(snapshot, fragments, tree, algebra, budget, BooleanAssignment.Empty, parentSizeContext: null);
        var rootTypeName = snapshot.GetSingletonObjectTypeName(tree.Root.Condition.PossibleTypes);
        var rootTypeWeight = snapshot.GetTypeWeight(rootTypeName);
        return MapRoot(algebra, rootTypeWeight, selection);
    }

    /// <summary>
    /// Maps <see cref="IAnalysisAlgebra{T}.Root"/> over every leaf of the
    /// root selection's possibly-split decision.
    /// </summary>
    private static BooleanDecision<TSummary> MapRoot<TSummary>(
        IAnalysisAlgebra<TSummary> algebra,
        double rootTypeWeight,
        BooleanDecision<TSummary> selection)
    {
        if (selection is LeafDecision<TSummary> leaf)
        {
            return BooleanDecision<TSummary>.Leaf(algebra.Root(rootTypeWeight, leaf.Value));
        }

        var split = (SplitDecision<TSummary>)selection;
        return BooleanDecision<TSummary>.Split(
            split.Variable,
            MapRoot(algebra, rootTypeWeight, split.WhenFalse),
            MapRoot(algebra, rootTypeWeight, split.WhenTrue));
    }

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
        BooleanAssignment assignment,
        SizedFieldContext? parentSizeContext)
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
            var regionResult = EvaluateCase(snapshot, fragments, tree, algebra, budget, region, representative, assignment, parentSizeContext);
            combined = combined is null
                ? regionResult
                : BooleanDecision<TSummary>.ZipWith(combined, regionResult, algebra.Join, algebra.Join, budget);
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
        BooleanAssignment assignment,
        SizedFieldContext? parentSizeContext)
    {
        var visited = new List<int>();
        var visitedSet = new HashSet<int>();
        var pending = new List<BooleanLiteral>();
        CollectReachable(tree, representative, assignment, tree.RootNodeId, visited, visitedSet, pending);

        if (pending.Count == 0)
        {
            return CollectAndWeigh(snapshot, fragments, tree, algebra, budget, region, assignment, visited, parentSizeContext);
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
                parentSizeContext);
        }

        var whenFalse = EvaluateCase(snapshot, fragments, tree, algebra, budget, region, representative, assignment.With(variable, false), parentSizeContext);
        var whenTrue = EvaluateCase(snapshot, fragments, tree, algebra, budget, region, representative, assignment.With(variable, true), parentSizeContext);
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
        List<int> visited,
        SizedFieldContext? parentSizeContext)
    {
        BooleanDecision<TSummary>? combined = null;

        foreach (var (responseName, fields) in FieldGroupMerger.Merge(tree, visited))
        {
            var fieldName = fields[0].Name.Value;
            var members = TraversalMembers.Build(snapshot, region, fieldName);

            if (members.Length == 0)
            {
                continue;
            }

            var childSelections = FieldGroupMerger.MergedSelections(fields);
            BooleanDecision<TSummary>? groupDecision = null;

            foreach (var field in fields)
            {
                foreach (var member in members)
                {
                    var childSizeContext = InheritedListSizes.Resolve(snapshot, member, field.Arguments);
                    var childDecision = childSelections.Count == 0
                        ? BooleanDecision<TSummary>.Leaf(algebra.Empty)
                        : EvaluateChild(snapshot, fragments, algebra, budget, assignment, member, childSelections, childSizeContext);
                    var pairDecision = MapField(
                        algebra,
                        responseName,
                        field,
                        member,
                        InheritedListSizes.InheritedSizeFor(parentSizeContext, fieldName),
                        childDecision);

                    groupDecision = groupDecision is null
                        ? pairDecision
                        : BooleanDecision<TSummary>.ZipWith(groupDecision, pairDecision, algebra.Join, algebra.Join, budget);
                }
            }

            combined = combined is null
                ? groupDecision!
                : BooleanDecision<TSummary>.ZipWith(combined, groupDecision!, algebra.Combine, algebra.Join, budget);
        }

        return combined ?? BooleanDecision<TSummary>.Leaf(algebra.Empty);
    }

    /// <summary>
    /// Evaluates one child boundary for a field occurrence and parent-type
    /// pair using that pair's return type and list-size context.
    /// </summary>
    private static BooleanDecision<TSummary> EvaluateChild<TSummary>(
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
        return EvaluateBoundary(snapshot, fragments, childTree, algebra, budget, assignment, parentSizeContext);
    }

    /// <summary>
    /// Maps <see cref="IAnalysisAlgebra{T}.Field"/> over every leaf of one
    /// occurrence and parent-type pair's child decision.
    /// </summary>
    private static BooleanDecision<TSummary> MapField<TSummary>(
        IAnalysisAlgebra<TSummary> algebra,
        string responseName,
        FieldNode field,
        CollectedFieldGroupMember member,
        double? inheritedSize,
        BooleanDecision<TSummary> child)
    {
        if (child is LeafDecision<TSummary> leaf)
        {
            return BooleanDecision<TSummary>.Leaf(
                algebra.Field(new CollectedFieldGroup(responseName, field, member, inheritedSize), leaf.Value));
        }

        var split = (SplitDecision<TSummary>)child;
        return BooleanDecision<TSummary>.Split(
            split.Variable,
            MapField(algebra, responseName, field, member, inheritedSize, split.WhenFalse),
            MapField(algebra, responseName, field, member, inheritedSize, split.WhenTrue));
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
}
