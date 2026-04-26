using System.Collections.Immutable;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

/// <summary>
/// Walks an operation AST and records every leaf field together with the
/// enclosing <c>@defer</c> chain leaf. The resulting list is the single
/// source of truth for downstream passes that compute effective defer
/// usage sets and emit per-set subplans. The walk consults the
/// <see cref="DeferPartitioningResult.ByFragment"/> map so every leaf
/// reference shares object identity with the planner pipeline.
/// </summary>
internal static class DeferOccurrenceCollector
{
    /// <summary>
    /// Collects leaf field occurrences from the given operation, optionally
    /// folding unlabeled nested <c>@defer</c> fragments into their parent
    /// when the wire output would be indistinguishable.
    /// </summary>
    /// <param name="operation">The operation definition to walk.</param>
    /// <param name="byFragment">
    /// The canonical <see cref="InlineFragmentNode"/> to <see cref="DeliveryGroup"/>
    /// lookup produced by <see cref="DeferPartitioner"/>.
    /// </param>
    /// <param name="inlineUnlabeledNestedDefers">
    /// When <c>true</c> an unlabeled nested <c>@defer</c> whose <c>if</c>
    /// variable matches its parent is folded into the parent's set.
    /// </param>
    public static List<FieldOccurrence> Collect(
        OperationDefinitionNode operation,
        IReadOnlyDictionary<InlineFragmentNode, DeliveryGroup> byFragment,
        bool inlineUnlabeledNestedDefers)
    {
        var occurrences = new List<FieldOccurrence>();
        CollectOccurrences(
            operation.SelectionSet.Selections,
            parentPath: [],
            enclosingDefer: null,
            parentTypeCondition: null,
            byFragment,
            inlineUnlabeledNestedDefers,
            occurrences);
        return occurrences;
    }

    private static void CollectOccurrences(
        IReadOnlyList<ISelectionNode> selections,
        ImmutableArray<FieldPathSegment> parentPath,
        DeliveryGroup? enclosingDefer,
        NamedTypeNode? parentTypeCondition,
        IReadOnlyDictionary<InlineFragmentNode, DeliveryGroup> byFragment,
        bool inlineUnlabeledNestedDefers,
        List<FieldOccurrence> occurrences)
    {
        foreach (var selection in selections)
        {
            if (selection is FieldNode fieldNode)
            {
                if (fieldNode.SelectionSet is { } childSelectionSet)
                {
                    // Composite field: we do NOT record an occurrence at this
                    // level. The wrapping is reconstructed by the subplan AST
                    // builder from the path tree, which looks up the original
                    // field (with its arguments/alias/directives) in the
                    // source AST. Recording an occurrence here would lead to
                    // the field being emitted twice: once as a wrapper and
                    // once as a leaf contribution.
                    var childPath = parentPath.Add(
                        new FieldPathSegment(
                            fieldNode.Name.Value,
                            fieldNode.Alias?.Value));

                    CollectOccurrences(
                        childSelectionSet.Selections,
                        childPath,
                        enclosingDefer,
                        parentTypeCondition: null,
                        byFragment,
                        inlineUnlabeledNestedDefers,
                        occurrences);
                }
                else
                {
                    // Leaf field: add as a direct contribution at the current
                    // path. Effective-set computation groups these by
                    // (parentPath, responseName) so sibling @defer fragments
                    // that share a leaf are unified into a single subplan.
                    occurrences.Add(
                        new FieldOccurrence(
                            parentPath,
                            fieldNode.Alias?.Value ?? fieldNode.Name.Value,
                            fieldNode,
                            enclosingDefer,
                            parentTypeCondition));
                }

                continue;
            }

            if (selection is InlineFragmentNode inlineFragment)
            {
                var nestedDefer = enclosingDefer;

                if (byFragment.TryGetValue(inlineFragment, out var canonical))
                {
                    // Fragment is a @defer. Honor the unlabeled-inlining option:
                    // an unlabeled nested @defer whose condition matches its
                    // parent's is indistinguishable from the parent in the wire
                    // output, so we fold its fields into the parent's set.
                    if (inlineUnlabeledNestedDefers
                        && canonical.Label is null
                        && enclosingDefer is not null
                        && (canonical.IfVariable is null
                            || canonical.IfVariable == enclosingDefer.IfVariable))
                    {
                        // Treat as non-defer: keep enclosingDefer.
                    }
                    else
                    {
                        nestedDefer = canonical;
                    }
                }

                CollectOccurrences(
                    inlineFragment.SelectionSet.Selections,
                    parentPath,
                    nestedDefer,
                    inlineFragment.TypeCondition ?? parentTypeCondition,
                    byFragment,
                    inlineUnlabeledNestedDefers,
                    occurrences);
            }
        }
    }
}
