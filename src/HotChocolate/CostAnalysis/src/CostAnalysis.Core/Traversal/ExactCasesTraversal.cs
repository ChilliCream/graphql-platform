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
        while (cursor.TryPeek(out var branch, out var rest))
        {
            if (branch.Condition.TypeName is not null)
            {
                cursor = tree.Nodes[branch.TargetNodeId].Condition.PossibleTypes.Contains(representative)
                    ? cursor.Select(branch.TargetNodeId, rest)
                    : cursor.Skip(rest);
                continue;
            }

            var literal = branch.Condition.Literal!.Value;

            if (assignment.TryGetValue(literal.VariableName, out var value))
            {
                cursor = cursor.ResolveBoolean(branch.TargetNodeId, rest, literal, value);
                continue;
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
                assignment.With(literal.VariableName, false),
                cursor.ResolveBoolean(branch.TargetNodeId, rest, literal, false),
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
                assignment.With(literal.VariableName, true),
                cursor.ResolveBoolean(branch.TargetNodeId, rest, literal, true),
                cache,
                parentSizeContext);
            return BooleanDecision<TSummary>.Split(literal.VariableName, whenFalse, whenTrue);
        }

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
        if (cache.HasUniqueResponseNames(tree))
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

        BooleanDecision<TSummary>? combined = null;

        foreach (var (responseName, fields) in FieldGroupMerger.Merge(tree, visited))
        {
            var fieldName = fields[0].Name.Value;
            var members = cache.GetMembers(region, fieldName);

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
                    var pairDecision = MapField(
                        algebra,
                        responseName,
                        field,
                        member,
                        parentSizeContext,
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
                        var pairDecision = MapField(
                            algebra,
                            group.ResponseName,
                            field,
                            member,
                            parentSizeContext,
                            childDecision);

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
            var group = new CollectedFieldGroup(
                responseName,
                field,
                member,
                InheritedListSizes.InheritedSizeFor(inheritedSizeContext, member.Field.Name));
            var value = algebra is IInheritedSizePlanAlgebra<TSummary> planAlgebra
                ? planAlgebra.Field(group, inheritedSizeContext, leaf.Value)
                : algebra.Field(group, leaf.Value);
            return BooleanDecision<TSummary>.Leaf(value);
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

    private readonly struct CaseCursor
    {
        private readonly ConditionTree _tree;
        private readonly SelectedNode _selected;
        private readonly PendingBranches _pending;

        private CaseCursor(
            ConditionTree tree,
            SelectedNode selected,
            PendingBranches pending)
        {
            _tree = tree;
            _selected = selected;
            _pending = pending;
        }

        public static CaseCursor Create(ConditionTree tree)
            => new(
                tree,
                new SelectedNode(tree.RootNodeId, previous: null),
                PendingBranches.Prepend(tree.Root.Branches, default));

        public bool TryPeek(out Branch branch, out PendingBranches rest)
        {
            if (_pending.IsEmpty)
            {
                branch = default;
                rest = default;
                return false;
            }

            branch = _pending.Current;
            rest = _pending.Rest();
            return true;
        }

        public CaseCursor Skip(PendingBranches rest)
            => new(_tree, _selected, rest);

        public CaseCursor Select(int nodeId, PendingBranches rest)
        {
            if (_selected.Contains(nodeId))
            {
                return Skip(rest);
            }

            var selected = new SelectedNode(nodeId, _selected);
            var pending = PendingBranches.Prepend(_tree.Nodes[nodeId].Branches, rest);
            return new CaseCursor(_tree, selected, pending);
        }

        public CaseCursor ResolveBoolean(
            int nodeId,
            PendingBranches rest,
            BooleanLiteral literal,
            bool value)
            => literal.IsPositive == value ? Select(nodeId, rest) : Skip(rest);

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
        private readonly Dictionary<ConditionTree, bool> _uniqueResponseNames = [];

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

        public bool HasUniqueResponseNames(ConditionTree tree)
        {
            if (_uniqueResponseNames.TryGetValue(tree, out var unique))
            {
                return unique;
            }

            var names = new HashSet<string>();
            unique = true;

            foreach (var node in tree.Nodes)
            {
                foreach (var group in node.FieldGroups)
                {
                    if (!names.Add(group.ResponseName))
                    {
                        unique = false;
                        break;
                    }
                }

                if (!unique)
                {
                    break;
                }
            }

            _uniqueResponseNames.Add(tree, unique);
            return unique;
        }

        public bool TryCountIndependentLeafVariables(
            ConditionTree tree,
            int representative,
            BooleanAssignment assignment,
            out int count)
        {
            if (!HasUniqueResponseNames(tree))
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
