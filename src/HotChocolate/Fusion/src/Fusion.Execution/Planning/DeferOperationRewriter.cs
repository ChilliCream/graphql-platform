using System.Collections.Immutable;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Planning;

/// <summary>
/// Splits an operation with <c>@defer</c> directives into a main operation
/// (non-deferred fields only) and one subplan operation per unique
/// <see cref="DeliveryGroup"/> set. The set keying follows the GraphQL
/// incremental-delivery spec: each field's active defer usage set is the
/// union of its per-occurrence enclosing <c>@defer</c> leaves (with
/// parent-child pruning). Two sibling <c>... @defer</c> fragments that share
/// a field therefore produce one subplan keyed by both usages for the shared
/// field, rather than two independent subplans that both fetch it.
/// </summary>
internal sealed class DeferOperationRewriter
{
    private readonly bool _inlineUnlabeledNestedDefers;

    internal DeferOperationRewriter(bool inlineUnlabeledNestedDefers = true)
    {
        _inlineUnlabeledNestedDefers = inlineUnlabeledNestedDefers;
    }

    /// <summary>
    /// Fast check whether the operation contains any <c>@defer</c> directives.
    /// Used to avoid the full split for non-deferred operations (the common case).
    /// </summary>
    public static bool HasDeferDirective(OperationDefinitionNode operation)
    {
        return HasDeferInSelectionSet(operation.SelectionSet);

        static bool HasDeferInSelectionSet(SelectionSetNode selectionSet)
        {
            for (var i = 0; i < selectionSet.Selections.Count; i++)
            {
                var selection = selectionSet.Selections[i];

                if (selection is InlineFragmentNode inlineFragment)
                {
                    if (HasDeferDirective(inlineFragment))
                    {
                        return true;
                    }

                    if (HasDeferInSelectionSet(inlineFragment.SelectionSet))
                    {
                        return true;
                    }
                }
                else if (selection is FragmentSpreadNode fragmentSpread)
                {
                    if (HasDeferDirectiveOnSpread(fragmentSpread))
                    {
                        return true;
                    }
                }
                else if (selection is FieldNode { SelectionSet: not null } field)
                {
                    if (HasDeferInSelectionSet(field.SelectionSet))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        static bool HasDeferDirectiveOnSpread(FragmentSpreadNode node)
        {
            for (var i = 0; i < node.Directives.Count; i++)
            {
                if (node.Directives[i].Name.Value.Equals(
                    DirectiveNames.Defer.Name,
                    StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Splits the given operation at <c>@defer</c> boundaries using the
    /// <see cref="DeliveryGroup"/> topology produced by
    /// <see cref="DeferPartitioner"/>. The output contains the stripped main
    /// operation (fields whose active <c>DeliveryGroupSet</c> is empty) plus one
    /// subplan per unique non-empty active set. Sibling <c>@defer</c>
    /// fragments that share a field collapse into a single subplan keyed by
    /// the union of both usages.
    /// </summary>
    /// <param name="operation">The operation definition that may contain @defer directives.</param>
    /// <param name="partitioning">The <see cref="DeferPartitioner"/> output for <paramref name="operation"/>.</param>
    public DeferSplitResult Split(
        OperationDefinitionNode operation,
        DeferPartitioningResult partitioning)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(partitioning);

        var occurrences = DeferOccurrenceCollector.Collect(
            operation,
            partitioning.ByFragment,
            _inlineUnlabeledNestedDefers);

        var effectiveSetByLocation = DeferEffectiveSetResolver.Resolve(occurrences);

        var mainOperation = BuildMainOperation(operation, partitioning.ByFragment);

        if (partitioning.AllDeliveryGroups.IsEmpty)
        {
            return new DeferSplitResult(mainOperation, []);
        }

        var subPlanDescriptors = BuildSubPlanOps(operation, occurrences, effectiveSetByLocation);

        return new DeferSplitResult(mainOperation, subPlanDescriptors);
    }

    private static ImmutableArray<IncrementalPlanDescriptor> BuildSubPlanOps(
        OperationDefinitionNode operation,
        List<FieldOccurrence> occurrences,
        Dictionary<FieldLocation, DeliveryGroupSetKey> effectiveSetByLocation)
    {
        // Bucket occurrences by effective set. We use a canonical
        // ImmutableArray<DeliveryGroup> (sorted by Id) as the set key so the
        // resulting buckets are stable across runs and trivially comparable
        // by sequence equality.
        var buckets = new Dictionary<DeliveryGroupSetKey, SubPlanBucket>();

        foreach (var occurrence in occurrences)
        {
            var key = effectiveSetByLocation[new FieldLocation(occurrence.ParentPath, occurrence.ResponseName)];

            if (key.IsEmpty)
            {
                continue;
            }

            if (!buckets.TryGetValue(key, out var bucket))
            {
                bucket = new SubPlanBucket(key);
                buckets[key] = bucket;
            }

            bucket.Add(occurrence);
        }

        if (buckets.Count == 0)
        {
            return [];
        }

        // Synthesize one OperationDefinitionNode per bucket. The output is a
        // query operation rooted at Query, with the wrapping path fields
        // looked up in the original AST so arguments, aliases and directives
        // are preserved. Buckets are ordered by the smallest DeliveryGroup Id in
        // the key so subsequent sort-by-Id consumers see a deterministic order.
        var ordered = buckets.Values.ToList();
        ordered.Sort(static (a, b) => a.Key.CompareTo(b.Key));

        var subPlanDescriptors = ImmutableArray.CreateBuilder<IncrementalPlanDescriptor>(ordered.Count);
        var descriptorByKey = new Dictionary<DeliveryGroupSetKey, IncrementalPlanDescriptor>(ordered.Count);

        foreach (var bucket in ordered)
        {
            var subPlanOp = BuildSubPlanOperation(operation, bucket);
            var path = DeterminePath(bucket.Key);
            var parent = ResolveParentDescriptor(bucket.Key, descriptorByKey);
            var descriptor = new IncrementalPlanDescriptor(
                deliveryGroupSet: bucket.Key.Items,
                operation: subPlanOp,
                path: path,
                parent: parent);

            descriptorByKey[bucket.Key] = descriptor;
            subPlanDescriptors.Add(descriptor);
        }

        return subPlanDescriptors.ToImmutable();
    }

    private OperationDefinitionNode BuildMainOperation(
        OperationDefinitionNode operation,
        IReadOnlyDictionary<InlineFragmentNode, DeliveryGroup> byFragment)
    {
        var newRoot = StripDeferFromSelectionSet(operation.SelectionSet, byFragment);
        return operation.WithSelectionSet(newRoot);
    }

    private SelectionSetNode StripDeferFromSelectionSet(
        SelectionSetNode selectionSet,
        IReadOnlyDictionary<InlineFragmentNode, DeliveryGroup> byFragment)
    {
        var selections = new List<ISelectionNode>(selectionSet.Selections.Count);
        var modified = false;

        for (var i = 0; i < selectionSet.Selections.Count; i++)
        {
            var selection = selectionSet.Selections[i];

            if (selection is InlineFragmentNode inlineFragment
                && byFragment.TryGetValue(inlineFragment, out var usage))
            {
                // The main operation never keeps @defer fragments: all
                // deferred fields go through their subplan. For a conditional
                // @defer(if: $variable) we still keep a @skip(if: $variable)
                // guarded inline copy so the variable-off runtime path
                // fetches the fields eagerly.
                if (usage.IfVariable is not null)
                {
                    var skipDirective = new DirectiveNode(
                        null,
                        new NameNode("skip"),
                        [
                            new ArgumentNode(
                                null,
                                new NameNode("if"),
                                new VariableNode(new NameNode(usage.IfVariable)))
                        ]);

                    var stripped = StripDeferDirective(inlineFragment);
                    var nested = StripDeferFromSelectionSet(stripped.SelectionSet, byFragment);

                    if (!ReferenceEquals(nested, stripped.SelectionSet))
                    {
                        stripped = stripped.WithSelectionSet(nested);
                    }

                    selections.Add(stripped.WithDirectives([.. stripped.Directives, skipDirective]));
                }

                modified = true;
                continue;
            }

            if (selection is InlineFragmentNode nonDeferFragment)
            {
                var nestedInner = StripDeferFromSelectionSet(nonDeferFragment.SelectionSet, byFragment);

                if (!ReferenceEquals(nestedInner, nonDeferFragment.SelectionSet))
                {
                    nonDeferFragment = nonDeferFragment.WithSelectionSet(nestedInner);
                    modified = true;
                }

                selections.Add(nonDeferFragment);
                continue;
            }

            if (selection is FieldNode fieldNode && fieldNode.SelectionSet is not null)
            {
                var childInner = StripDeferFromSelectionSet(fieldNode.SelectionSet, byFragment);

                if (!ReferenceEquals(childInner, fieldNode.SelectionSet))
                {
                    fieldNode = fieldNode.WithSelectionSet(childInner);
                    modified = true;
                }

                selections.Add(fieldNode);
                continue;
            }

            selections.Add(selection);
        }

        if (!modified)
        {
            return selectionSet;
        }

        if (selections.Count == 0)
        {
            selections.Add(new FieldNode("__typename"));
        }

        return new SelectionSetNode(selections);
    }

    private static OperationDefinitionNode BuildSubPlanOperation(
        OperationDefinitionNode rootOperation,
        SubPlanBucket bucket)
    {
        // Build a tree of PathNodes keyed by FieldPathSegment. Each leaf
        // carries the FieldNodes (per optional type condition) contributed
        // by the bucket at that path. We resolve each wrapping path field
        // against the original AST so arguments and aliases survive.
        var root = new PathNode();

        foreach (var occurrence in bucket.Occurrences)
        {
            var node = root;
            for (var i = 0; i < occurrence.ParentPath.Length; i++)
            {
                var segment = occurrence.ParentPath[i];
                node = node.GetOrAddChild(segment);
            }

            node.AddContribution(occurrence.ResponseName, occurrence.FieldNode, occurrence.TypeCondition);
        }

        var rootSelectionSet = BuildSelectionSetFromPathNode(
            root,
            rootOperation.SelectionSet,
            parentPath: []);

        return rootOperation
            .WithOperation(OperationType.Query)
            .WithDirectives([])
            .WithSelectionSet(rootSelectionSet);
    }

    private static SelectionSetNode BuildSelectionSetFromPathNode(
        PathNode node,
        SelectionSetNode originalSelectionSet,
        ImmutableArray<FieldPathSegment> parentPath)
    {
        var selections = new List<ISelectionNode>();

        // Leaf contributions at this path. Group by type condition name so
        // each `on Type` wrapping is emitted once. The same response name
        // may be contributed by multiple sibling @defer fragments; we only
        // keep the first to avoid duplicate selections in the subgraph
        // request.
        if (node.Contributions.Count > 0)
        {
            var unconditional = new List<FieldNode>();
            var byTypeCondition = new Dictionary<string, (NamedTypeNode Node, List<FieldNode> Fields)>(
                StringComparer.Ordinal);
            var seen = new HashSet<(string? TypeCondition, string ResponseName)>();

            foreach (var contribution in node.Contributions)
            {
                var discriminator = (contribution.TypeCondition?.Name.Value, contribution.ResponseName);
                if (!seen.Add(discriminator))
                {
                    continue;
                }

                if (contribution.TypeCondition is null)
                {
                    unconditional.Add(contribution.FieldNode);
                }
                else
                {
                    var typeName = contribution.TypeCondition.Name.Value;
                    if (!byTypeCondition.TryGetValue(typeName, out var bucketEntry))
                    {
                        bucketEntry = (contribution.TypeCondition, []);
                        byTypeCondition[typeName] = bucketEntry;
                    }

                    bucketEntry.Fields.Add(contribution.FieldNode);
                }
            }

            foreach (var field in unconditional)
            {
                selections.Add(field);
            }

            foreach (var (_, bucketEntry) in byTypeCondition)
            {
                selections.Add(new InlineFragmentNode(
                    null,
                    bucketEntry.Node,
                    [],
                    new SelectionSetNode(bucketEntry.Fields.ToArray<ISelectionNode>())));
            }
        }

        // Child path nodes: wrap in the original field node (preserving
        // name/alias/arguments/directives) so the subplan operation is a
        // syntactically valid query against the root schema.
        foreach (var (segment, childNode) in node.Children)
        {
            var wrappingField = ResolveWrappingField(originalSelectionSet, segment)
                ?? throw new InvalidOperationException(
                    $"Unable to resolve wrapping field for '{segment.ResponseName}' at path '{FormatPath(parentPath)}'.");

            var childSelectionSet = wrappingField.SelectionSet
                ?? throw new InvalidOperationException(
                    $"Wrapping field '{segment.ResponseName}' at path '{FormatPath(parentPath)}' has no selection set.");

            var childParentPath = parentPath.Add(segment);
            var nestedSelectionSet = BuildSelectionSetFromPathNode(childNode, childSelectionSet, childParentPath);

            selections.Add(new FieldNode(
                null,
                wrappingField.Name,
                wrappingField.Alias,
                wrappingField.Directives,
                wrappingField.Arguments,
                nestedSelectionSet));
        }

        if (selections.Count == 0)
        {
            selections.Add(new FieldNode("__typename"));
        }

        return new SelectionSetNode(selections);
    }

    private static FieldNode? ResolveWrappingField(
        SelectionSetNode selectionSet,
        FieldPathSegment segment)
    {
        for (var i = 0; i < selectionSet.Selections.Count; i++)
        {
            var selection = selectionSet.Selections[i];

            if (selection is FieldNode field)
            {
                var responseName = field.Alias?.Value ?? field.Name.Value;

                if (responseName.Equals(segment.ResponseName, StringComparison.Ordinal))
                {
                    return field;
                }
            }

            if (selection is InlineFragmentNode inline)
            {
                var nested = ResolveWrappingField(inline.SelectionSet, segment);

                if (nested is not null)
                {
                    return nested;
                }
            }
        }

        return null;
    }

    private static string FormatPath(ImmutableArray<FieldPathSegment> path)
    {
        if (path.IsEmpty)
        {
            return "$";
        }

        var builder = new System.Text.StringBuilder("$");
        for (var i = 0; i < path.Length; i++)
        {
            builder.Append('.');
            builder.Append(path[i].ResponseName);
        }

        return builder.ToString();
    }

    private static SelectionPath DeterminePath(DeliveryGroupSetKey key)
    {
        // The subplan roots at the longest shared ancestor of all usages in
        // its set. For siblings (same parent), every usage has the same
        // anchor path; for the general case we pick the deepest path because
        // after parent pruning no two usages in the same set sit on the same
        // parent chain.
        SelectionPath? best = null;

        foreach (var usage in key.Items)
        {
            if (usage.Path is null)
            {
                continue;
            }

            if (best is null || usage.Path.Length > best.Length)
            {
                best = usage.Path;
            }
        }

        return best ?? SelectionPath.Root;
    }

    private static IncrementalPlanDescriptor? ResolveParentDescriptor(
        DeliveryGroupSetKey key,
        Dictionary<DeliveryGroupSetKey, IncrementalPlanDescriptor> descriptorByKey)
    {
        // A subplan's "parent" is the subplan whose key contains the parent
        // DeliveryGroup of any usage in this set. We pick the first usage's
        // parent chain; all other usages in the set share an equivalent
        // ancestry after parent pruning.
        foreach (var usage in key.Items)
        {
            var parent = usage.Parent;
            while (parent is not null)
            {
                foreach (var (candidateKey, candidate) in descriptorByKey)
                {
                    foreach (var candidateUsage in candidateKey.Items)
                    {
                        if (ReferenceEquals(candidateUsage, parent))
                        {
                            return candidate;
                        }
                    }
                }
                parent = parent.Parent;
            }
        }

        return null;
    }

    private static InlineFragmentNode StripDeferDirective(InlineFragmentNode node)
    {
        var directives = new List<DirectiveNode>(node.Directives.Count);

        for (var i = 0; i < node.Directives.Count; i++)
        {
            if (!node.Directives[i].Name.Value.Equals(
                DirectiveNames.Defer.Name,
                StringComparison.Ordinal))
            {
                directives.Add(node.Directives[i]);
            }
        }

        return node.WithDirectives(directives);
    }

    private static bool HasDeferDirective(InlineFragmentNode node)
    {
        for (var i = 0; i < node.Directives.Count; i++)
        {
            if (node.Directives[i].Name.Value.Equals(
                DirectiveNames.Defer.Name,
                StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private sealed class SubPlanBucket(DeliveryGroupSetKey key)
    {
        public DeliveryGroupSetKey Key { get; } = key;
        public List<FieldOccurrence> Occurrences { get; } = [];

        public void Add(FieldOccurrence occurrence) => Occurrences.Add(occurrence);
    }

    private sealed class PathNode
    {
        public List<(string ResponseName, FieldNode FieldNode, NamedTypeNode? TypeCondition)> Contributions { get; } = [];
        public Dictionary<FieldPathSegment, PathNode> Children { get; } = [];

        public PathNode GetOrAddChild(FieldPathSegment segment)
        {
            if (!Children.TryGetValue(segment, out var child))
            {
                child = new PathNode();
                Children[segment] = child;
            }

            return child;
        }

        public void AddContribution(string responseName, FieldNode fieldNode, NamedTypeNode? typeCondition)
        {
            Contributions.Add((responseName, fieldNode, typeCondition));
        }
    }
}
