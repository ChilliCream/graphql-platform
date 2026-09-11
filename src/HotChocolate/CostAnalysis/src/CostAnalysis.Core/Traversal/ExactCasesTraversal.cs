using System.Buffers;
using System.Runtime.CompilerServices;
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
        var cache = new TraversalCache(snapshot, fragments);
        var selection = EvaluateBoundary(
            snapshot,
            fragments,
            tree,
            algebra,
            budget,
            BooleanAssignment.Empty,
            cache,
            parentSizeContext: null);
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

        if (selection is SplitDecision<TSummary> split)
        {
            return BooleanDecision<TSummary>.Split(
                split.Variable,
                MapRoot(algebra, rootTypeWeight, split.WhenFalse),
                MapRoot(algebra, rootTypeWeight, split.WhenTrue));
        }

        var joined = (JoinDecision<TSummary>)selection;
        return BooleanDecision<TSummary>.Join(
            MapRoot(algebra, rootTypeWeight, joined.Left),
            MapRoot(algebra, rootTypeWeight, joined.Right),
            algebra.Join);
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
        TraversalCache cache,
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

            if (cache.TryCountIndependentLeafVariables(
                    tree,
                    representative,
                    assignment,
                    out var independentVariables)
                && !budget.CanCompleteIndependentDecision(independentVariables))
            {
                var fallback = CaseBudgetFallback.Evaluate(
                    snapshot,
                    fragments,
                    tree,
                    algebra,
                    budget,
                    region,
                    representative,
                    assignment,
                    cache,
                    parentSizeContext);
                combined = combined is null
                    ? fallback
                    : BooleanDecision<TSummary>.Join(combined, fallback, algebra.Join);
                continue;
            }

            var cursor = CaseCursor.Create(tree);
            var regionResult = EvaluateCase(
                snapshot,
                fragments,
                tree,
                algebra,
                budget,
                region,
                representative,
                assignment,
                cursor,
                cache,
                parentSizeContext);
            combined = combined is null
                ? regionResult
                : BooleanDecision<TSummary>.Join(combined, regionResult, algebra.Join);
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
        CaseCursor cursor,
        TraversalCache cache,
        SizedFieldContext? parentSizeContext)
    {
        cursor = cursor.Normalize(representative, assignment);

        if (!cursor.TryPickCanonicalVariable(representative, assignment, cache, out var variable))
        {
            return CollectAndWeigh(
                snapshot,
                fragments,
                tree,
                algebra,
                budget,
                region,
                assignment,
                cursor.MaterializeVisited(),
                cache,
                parentSizeContext);
        }

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
                cache,
                parentSizeContext);
        }

        var whenFalse = EvaluateCase(
            snapshot,
            fragments,
            tree,
            algebra,
            budget,
            region,
            representative,
            assignment.With(variable, false),
            cursor,
            cache,
            parentSizeContext);
        var whenTrue = EvaluateCase(
            snapshot,
            fragments,
            tree,
            algebra,
            budget,
            region,
            representative,
            assignment.With(variable, true),
            cursor,
            cache,
            parentSizeContext);
        return BooleanDecision<TSummary>.Split(variable, whenFalse, whenTrue);
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
        IReadOnlyList<int> visited,
        TraversalCache cache,
        SizedFieldContext? parentSizeContext)
    {
        if (tree.HasUniqueResponseNames)
        {
            return CollectUniqueAndWeigh(
                snapshot,
                fragments,
                tree,
                algebra,
                budget,
                region,
                assignment,
                visited,
                cache,
                parentSizeContext);
        }

        var scratchLength = FieldGroupAccumulator.GetRequiredScratchLength(tree);
        int[]? rented = null;
        Span<int> scratch = scratchLength <= FieldGroupAccumulator.MaxStackScratchLength
            ? stackalloc int[scratchLength]
            : (rented = ArrayPool<int>.Shared.Rent(scratchLength));

        try
        {
            var accumulator = new FieldGroupAccumulator(tree, scratch);
            accumulator.Build(visited);
            return CollectMergedAndWeigh(
                snapshot,
                fragments,
                algebra,
                budget,
                region,
                assignment,
                cache,
                parentSizeContext,
                ref accumulator);
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<int>.Shared.Return(rented);
            }
        }
    }

    private static BooleanDecision<TSummary> CollectMergedAndWeigh<TSummary>(
        CostSchemaSnapshot snapshot,
        IReadOnlyDictionary<string, FragmentDefinitionNode> fragments,
        IAnalysisAlgebra<TSummary> algebra,
        CaseBudget budget,
        PossibleTypeSet region,
        BooleanAssignment assignment,
        TraversalCache cache,
        SizedFieldContext? parentSizeContext,
        ref FieldGroupAccumulator accumulator)
    {
        BooleanDecision<TSummary>? combined = null;

        for (var responseIndex = 0; responseIndex < accumulator.Count; responseIndex++)
        {
            var responseNameId = accumulator.GetResponseNameId(responseIndex);
            var firstEntry = accumulator.GetFirstEntry(responseNameId);
            var firstGroup = accumulator.GetGroup(firstEntry);
            var responseName = firstGroup.ResponseName;
            var fieldName = firstGroup.Fields[0].Name.Value;
            var members = cache.GetMembers(region, fieldName);

            if (members.Length == 0)
            {
                continue;
            }

            if (!accumulator.HasChildSelections(responseNameId))
            {
                var hasDirectLeafValue = false;
                var directLeafValue = default(TSummary)!;

                for (var entry = firstEntry;
                    entry >= 0;
                    entry = accumulator.GetNextEntry(entry))
                {
                    foreach (var field in accumulator.GetGroup(entry).Fields)
                    {
                        if (algebra is ILeafFieldBatchAlgebra<TSummary> batchAlgebra)
                        {
                            batchAlgebra.AccumulateField(
                                responseName,
                                field,
                                members,
                                parentSizeContext,
                                algebra.Empty,
                                ref hasDirectLeafValue,
                                ref directLeafValue);
                            continue;
                        }

                        foreach (var member in members)
                        {
                            _ = InheritedListSizes.Resolve(snapshot, member, field.Arguments);
                            var value = MapFieldValue(
                                algebra,
                                responseName,
                                field,
                                member,
                                parentSizeContext,
                                algebra.Empty);
                            directLeafValue = hasDirectLeafValue
                                ? algebra.Join(directLeafValue, value)
                                : value;
                            hasDirectLeafValue = true;
                        }
                    }
                }

                var leafDecision = BooleanDecision<TSummary>.Leaf(directLeafValue);
                combined = combined is null
                    ? leafDecision
                    : BooleanDecision<TSummary>.ZipWith(
                        combined,
                        leafDecision,
                        algebra.Combine,
                        algebra.Join,
                        budget);
                continue;
            }

            var fields = accumulator.MaterializeFields(responseNameId);
            var childSelections = FieldGroupMerger.MergedSelections(fields);
            BooleanDecision<TSummary>? groupDecision = null;
            var hasLeafValue = false;
            var leafValue = default(TSummary)!;

            foreach (var field in fields)
            {
                foreach (var member in members)
                {
                    var childSizeContext = InheritedListSizes.Resolve(snapshot, member, field.Arguments);
                    var childDecision = childSelections.Count == 0
                        ? BooleanDecision<TSummary>.Leaf(algebra.Empty)
                        : EvaluateChild(
                            snapshot,
                            fragments,
                            algebra,
                            budget,
                            assignment,
                            member,
                            fields,
                            childSelections,
                            cache,
                            childSizeContext);

                    if (groupDecision is null && childDecision is LeafDecision<TSummary> leaf)
                    {
                        var value = MapFieldValue(
                            algebra,
                            responseName,
                            field,
                            member,
                            parentSizeContext,
                            leaf.Value);
                        leafValue = hasLeafValue ? algebra.Join(leafValue, value) : value;
                        hasLeafValue = true;
                        continue;
                    }

                    var pairDecision = MapField(
                        algebra,
                        responseName,
                        field,
                        member,
                        parentSizeContext,
                        childDecision);

                    if (groupDecision is null && hasLeafValue)
                    {
                        groupDecision = BooleanDecision<TSummary>.Leaf(leafValue);
                    }

                    groupDecision = groupDecision is null
                        ? pairDecision
                        : BooleanDecision<TSummary>.ZipWith(groupDecision, pairDecision, algebra.Join, algebra.Join, budget);
                }
            }

            groupDecision ??= BooleanDecision<TSummary>.Leaf(leafValue);

            combined = combined is null
                ? groupDecision!
                : BooleanDecision<TSummary>.ZipWith(combined, groupDecision!, algebra.Combine, algebra.Join, budget);
        }

        return combined ?? BooleanDecision<TSummary>.Leaf(algebra.Empty);
    }

    private static BooleanDecision<TSummary> CollectUniqueAndWeigh<TSummary>(
        CostSchemaSnapshot snapshot,
        IReadOnlyDictionary<string, FragmentDefinitionNode> fragments,
        ConditionTree tree,
        IAnalysisAlgebra<TSummary> algebra,
        CaseBudget budget,
        PossibleTypeSet region,
        BooleanAssignment assignment,
        IReadOnlyList<int> visited,
        TraversalCache cache,
        SizedFieldContext? parentSizeContext)
    {
        BooleanDecision<TSummary>? combined = null;

        foreach (var nodeId in visited)
        {
            foreach (var group in tree.Nodes[nodeId].FieldGroups)
            {
                var fields = group.Fields;
                var fieldName = fields[0].Name.Value;
                var members = cache.GetMembers(region, fieldName);

                if (members.Length == 0)
                {
                    continue;
                }

                var childSelections = group.MergedSelectionSet();
                BooleanDecision<TSummary>? groupDecision = null;
                var hasLeafValue = false;
                var leafValue = default(TSummary)!;

                foreach (var field in fields)
                {
                    foreach (var member in members)
                    {
                        var childSizeContext = InheritedListSizes.Resolve(snapshot, member, field.Arguments);
                        var childDecision = childSelections.Count == 0
                            ? BooleanDecision<TSummary>.Leaf(algebra.Empty)
                            : EvaluateChild(
                                snapshot,
                                fragments,
                                algebra,
                                budget,
                                assignment,
                                member,
                                fields,
                                childSelections,
                                cache,
                                childSizeContext);

                        if (groupDecision is null && childDecision is LeafDecision<TSummary> leaf)
                        {
                            var value = MapFieldValue(
                                algebra,
                                group.ResponseName,
                                field,
                                member,
                                parentSizeContext,
                                leaf.Value);
                            leafValue = hasLeafValue ? algebra.Join(leafValue, value) : value;
                            hasLeafValue = true;
                            continue;
                        }

                        var pairDecision = MapField(
                            algebra,
                            group.ResponseName,
                            field,
                            member,
                            parentSizeContext,
                            childDecision);

                        if (groupDecision is null && hasLeafValue)
                        {
                            groupDecision = BooleanDecision<TSummary>.Leaf(leafValue);
                        }

                        groupDecision = groupDecision is null
                            ? pairDecision
                            : BooleanDecision<TSummary>.ZipWith(
                                groupDecision,
                                pairDecision,
                                algebra.Join,
                                algebra.Join,
                                budget);
                    }
                }

                groupDecision ??= BooleanDecision<TSummary>.Leaf(leafValue);

                combined = combined is null
                    ? groupDecision!
                    : BooleanDecision<TSummary>.ZipWith(
                        combined,
                        groupDecision!,
                        algebra.Combine,
                        algebra.Join,
                        budget);
            }
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
        IReadOnlyList<FieldNode> fields,
        IReadOnlyList<ISelectionNode> childSelections,
        TraversalCache cache,
        SizedFieldContext? parentSizeContext)
    {
        var returnTypeName = member.Field.Type.NamedType().Name;
        var childTree = cache.GetChildBoundary(returnTypeName, fields, childSelections);
        return EvaluateBoundary(
            snapshot,
            fragments,
            childTree,
            algebra,
            budget,
            assignment,
            cache,
            parentSizeContext);
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
        SizedFieldContext? inheritedSizeContext,
        BooleanDecision<TSummary> child)
    {
        if (child is LeafDecision<TSummary> leaf)
        {
            return BooleanDecision<TSummary>.Leaf(MapFieldValue(
                algebra,
                responseName,
                field,
                member,
                inheritedSizeContext,
                leaf.Value));
        }

        if (child is SplitDecision<TSummary> split)
        {
            return BooleanDecision<TSummary>.Split(
                split.Variable,
                MapField(algebra, responseName, field, member, inheritedSizeContext, split.WhenFalse),
                MapField(algebra, responseName, field, member, inheritedSizeContext, split.WhenTrue));
        }

        var joined = (JoinDecision<TSummary>)child;
        return BooleanDecision<TSummary>.Join(
            MapField(algebra, responseName, field, member, inheritedSizeContext, joined.Left),
            MapField(algebra, responseName, field, member, inheritedSizeContext, joined.Right),
            algebra.Join);
    }

    private static TSummary MapFieldValue<TSummary>(
        IAnalysisAlgebra<TSummary> algebra,
        string responseName,
        FieldNode field,
        CollectedFieldGroupMember member,
        SizedFieldContext? inheritedSizeContext,
        TSummary child)
    {
        var group = new CollectedFieldGroup(
            responseName,
            field,
            member,
            InheritedListSizes.InheritedSizeFor(inheritedSizeContext, member.Field.Name));
        return algebra is IInheritedSizePlanAlgebra<TSummary> planAlgebra
            ? planAlgebra.Field(group, inheritedSizeContext, child)
            : algebra.Field(group, child);
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

        result.Sort(CompareTypeSets);
        return result;
    }

    private static int CompareTypeSets(PossibleTypeSet left, PossibleTypeSet right)
    {
        var leftEnumerator = left.GetEnumerator();
        var rightEnumerator = right.GetEnumerator();

        while (true)
        {
            var hasLeft = leftEnumerator.MoveNext();
            var hasRight = rightEnumerator.MoveNext();

            if (!hasLeft || !hasRight)
            {
                return hasLeft == hasRight ? 0 : hasLeft ? 1 : -1;
            }

            var comparison = leftEnumerator.Current.CompareTo(rightEnumerator.Current);

            if (comparison != 0)
            {
                return comparison;
            }
        }
    }

    private readonly struct CaseCursor
    {
        private readonly ConditionTree _tree;
        private readonly SelectedNode _selected;
        private readonly PendingBranches _pending;
        private readonly DeferredBranch? _deferred;

        private CaseCursor(
            ConditionTree tree,
            SelectedNode selected,
            PendingBranches pending,
            DeferredBranch? deferred)
        {
            _tree = tree;
            _selected = selected;
            _pending = pending;
            _deferred = deferred;
        }

        public static CaseCursor Create(ConditionTree tree)
            => new(
                tree,
                new SelectedNode(tree.RootNodeId, previous: null),
                PendingBranches.Prepend(tree.Root.Branches, default),
                deferred: null);

        public CaseCursor Normalize(
            int representative,
            BooleanAssignment assignment)
        {
            var selected = _selected;
            var pending = _pending;
            var deferred = _deferred;
            DeferredBranch? unresolved = null;

            while (!pending.IsEmpty || deferred is not null)
            {
                Branch branch;

                if (!pending.IsEmpty)
                {
                    branch = pending.Current;
                    pending = pending.Rest();
                }
                else
                {
                    branch = deferred!.Branch;
                    deferred = deferred.Next;
                }

                if (selected.Contains(branch.TargetNodeId))
                {
                    continue;
                }

                if (branch.Condition.TypeName is not null)
                {
                    if (_tree.Nodes[branch.TargetNodeId].Condition.PossibleTypes.Contains(representative))
                    {
                        Select(branch.TargetNodeId, ref selected, ref pending);
                    }

                    continue;
                }

                var literal = branch.Condition.Literal!.Value;

                if (assignment.TryGetValue(literal.VariableName, out var value))
                {
                    if (literal.IsPositive == value)
                    {
                        Select(branch.TargetNodeId, ref selected, ref pending);
                    }

                    continue;
                }

                unresolved = new DeferredBranch(branch, unresolved);
            }

            return new CaseCursor(_tree, selected, default, unresolved);
        }

        public bool TryPickCanonicalVariable(
            int representative,
            BooleanAssignment assignment,
            TraversalCache cache,
            out string variable)
        {
            string? best = null;
            var scan = cache.BeginCanonicalScan(_tree);

            for (var deferred = _deferred; deferred is not null; deferred = deferred.Next)
            {
                var literal = deferred.Branch.Condition.Literal!.Value;
                PickEarlier(literal.VariableName, ref best);
                ScanPotentiallyReachable(
                    deferred.Branch.TargetNodeId,
                    representative,
                    assignment,
                    scan,
                    ref best);
            }

            variable = best!;
            return best is not null;
        }

        private void ScanPotentiallyReachable(
            int nodeId,
            int representative,
            BooleanAssignment assignment,
            CanonicalScan scan,
            ref string? best)
        {
            if (!scan.TryVisit(nodeId))
            {
                return;
            }

            foreach (var branch in _tree.Nodes[nodeId].Branches)
            {
                if (branch.Condition.TypeName is not null)
                {
                    if (_tree.Nodes[branch.TargetNodeId].Condition.PossibleTypes.Contains(representative))
                    {
                        ScanPotentiallyReachable(
                            branch.TargetNodeId,
                            representative,
                            assignment,
                            scan,
                            ref best);
                    }

                    continue;
                }

                var literal = branch.Condition.Literal!.Value;

                if (assignment.TryGetValue(literal.VariableName, out var value))
                {
                    if (literal.IsPositive == value)
                    {
                        ScanPotentiallyReachable(
                            branch.TargetNodeId,
                            representative,
                            assignment,
                            scan,
                            ref best);
                    }

                    continue;
                }

                PickEarlier(literal.VariableName, ref best);
                ScanPotentiallyReachable(
                    branch.TargetNodeId,
                    representative,
                    assignment,
                    scan,
                    ref best);
            }
        }

        private static void PickEarlier(string candidate, ref string? best)
        {
            if (best is null || string.CompareOrdinal(candidate, best) < 0)
            {
                best = candidate;
            }
        }

        private void Select(
            int nodeId,
            ref SelectedNode selected,
            ref PendingBranches pending)
        {
            selected = new SelectedNode(nodeId, selected);
            pending = PendingBranches.Prepend(_tree.Nodes[nodeId].Branches, pending);
        }

        public int[] MaterializeVisited()
        {
            var count = 0;

            for (var selected = _selected; selected is not null; selected = selected.Previous)
            {
                count++;
            }

            var result = new int[count];
            var index = count;

            for (var selected = _selected; selected is not null; selected = selected.Previous)
            {
                result[--index] = selected.NodeId;
            }

            return result;
        }
    }

    private sealed class DeferredBranch(Branch branch, DeferredBranch? next)
    {
        public Branch Branch { get; } = branch;

        public DeferredBranch? Next { get; } = next;
    }

    private sealed class SelectedNode(int nodeId, SelectedNode? previous)
    {
        public int NodeId { get; } = nodeId;

        public SelectedNode? Previous { get; } = previous;

        public bool Contains(int nodeId)
        {
            for (SelectedNode? selected = this; selected is not null; selected = selected.Previous)
            {
                if (selected.NodeId == nodeId)
                {
                    return true;
                }
            }

            return false;
        }
    }

    private readonly struct PendingBranches
    {
        private readonly IReadOnlyList<Branch>? _branches;
        private readonly int _index;
        private readonly PendingContinuation? _continuation;

        private PendingBranches(
            IReadOnlyList<Branch> branches,
            int index,
            PendingContinuation? continuation)
        {
            _branches = branches;
            _index = index;
            _continuation = continuation;
        }

        public bool IsEmpty => _branches is null;

        public Branch Current => _branches![_index];

        public static PendingBranches Prepend(
            IReadOnlyList<Branch> branches,
            PendingBranches rest)
        {
            if (branches.Count == 0)
            {
                return rest;
            }

            return new PendingBranches(
                branches,
                0,
                rest.IsEmpty ? null : new PendingContinuation(rest));
        }

        public PendingBranches Rest()
        {
            if (_index + 1 < _branches!.Count)
            {
                return new PendingBranches(_branches, _index + 1, _continuation);
            }

            return _continuation?.Value ?? default;
        }
    }

    private sealed class PendingContinuation(PendingBranches value)
    {
        public PendingBranches Value { get; } = value;
    }

    internal sealed class TraversalCache(
        CostSchemaSnapshot snapshot,
        IReadOnlyDictionary<string, FragmentDefinitionNode> fragments)
    {
        private readonly Dictionary<(PossibleTypeSet Region, string FieldName), CollectedFieldGroupMember[]> _members = [];
        private readonly Dictionary<ChildBoundaryKey, ConditionTree> _childBoundaries = new(ChildBoundaryKeyComparer.Instance);
        private readonly Dictionary<ConditionTree, CanonicalScanState> _canonicalScans = [];

        public CanonicalScan BeginCanonicalScan(ConditionTree tree)
        {
            if (!_canonicalScans.TryGetValue(tree, out var state))
            {
                state = new CanonicalScanState(tree.Nodes.Count);
                _canonicalScans.Add(tree, state);
            }

            return state.Begin();
        }

        public CollectedFieldGroupMember[] GetMembers(PossibleTypeSet region, string fieldName)
        {
            var key = (region, fieldName);

            if (!_members.TryGetValue(key, out var members))
            {
                members = TraversalMembers.Build(snapshot, region, fieldName);
                _members.Add(key, members);
            }

            return members;
        }

        public ConditionTree GetChildBoundary(
            string returnTypeName,
            IReadOnlyList<FieldNode> fields,
            IReadOnlyList<ISelectionNode> childSelections)
        {
            var key = new ChildBoundaryKey(returnTypeName, fields);

            if (!_childBoundaries.TryGetValue(key, out var tree))
            {
                var childRoot = new Condition(snapshot.GetPossibleTypeSet(returnTypeName), []);
                tree = ConditionTreeExtractor.ExtractBoundary(
                    snapshot,
                    fragments,
                    childSelections,
                    childRoot);
                _childBoundaries.Add(key, tree);
            }

            return tree;
        }

        public bool TryCountIndependentLeafVariables(
            ConditionTree tree,
            int representative,
            BooleanAssignment assignment,
            out int count)
        {
            if (!tree.HasUniqueResponseNames)
            {
                count = 0;
                return false;
            }

            var variables = new HashSet<string>();
            var visited = new HashSet<int>();
            var independent = Visit(tree.RootNodeId);
            count = variables.Count;
            return independent && count > 0;

            bool Visit(int nodeId)
            {
                if (!visited.Add(nodeId))
                {
                    return true;
                }

                foreach (var branch in tree.Nodes[nodeId].Branches)
                {
                    var body = tree.Nodes[branch.TargetNodeId];

                    if (branch.Condition.TypeName is not null)
                    {
                        if (body.Condition.PossibleTypes.Contains(representative)
                            && !Visit(branch.TargetNodeId))
                        {
                            return false;
                        }

                        continue;
                    }

                    var literal = branch.Condition.Literal!.Value;

                    if (assignment.TryGetValue(literal.VariableName, out _))
                    {
                        continue;
                    }

                    if (body.Branches.Count != 0
                        || body.FieldGroups.Count == 0
                        || !variables.Add(literal.VariableName))
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }

    internal readonly struct CanonicalScan(int[] visited, int stamp)
    {
        public bool TryVisit(int nodeId)
        {
            if (visited[nodeId] == stamp)
            {
                return false;
            }

            visited[nodeId] = stamp;
            return true;
        }
    }

    private sealed class CanonicalScanState(int nodeCount)
    {
        private readonly int[] _visited = new int[nodeCount];
        private int _stamp;

        public CanonicalScan Begin()
        {
            if (_stamp == int.MaxValue)
            {
                Array.Clear(_visited);
                _stamp = 0;
            }

            return new CanonicalScan(_visited, ++_stamp);
        }
    }

    private readonly record struct ChildBoundaryKey(
        string ReturnTypeName,
        IReadOnlyList<FieldNode> Fields);

    private sealed class ChildBoundaryKeyComparer : IEqualityComparer<ChildBoundaryKey>
    {
        public static readonly ChildBoundaryKeyComparer Instance = new();

        public bool Equals(ChildBoundaryKey x, ChildBoundaryKey y)
        {
            if (x.ReturnTypeName != y.ReturnTypeName || x.Fields.Count != y.Fields.Count)
            {
                return false;
            }

            for (var i = 0; i < x.Fields.Count; i++)
            {
                if (!ReferenceEquals(x.Fields[i], y.Fields[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public int GetHashCode(ChildBoundaryKey key)
        {
            var hash = new HashCode();
            hash.Add(key.ReturnTypeName, StringComparer.Ordinal);

            foreach (var field in key.Fields)
            {
                hash.Add(RuntimeHelpers.GetHashCode(field));
            }

            return hash.ToHashCode();
        }
    }
}
