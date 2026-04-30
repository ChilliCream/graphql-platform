using System.Collections.Immutable;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

/// <summary>
/// Collects leaf field occurrences together with their enclosing delivery
/// group. The collected occurrences are grouped later by effective delivery
/// group set to produce incremental plans.
/// </summary>
internal static class DeferOccurrenceCollector
{
    /// <summary>
    /// Collects leaf field occurrences from the given operation, optionally
    /// folding equivalent unlabeled nested <c>@defer</c> fragments into their
    /// parent delivery group.
    /// </summary>
    /// <param name="operation">The operation definition to walk.</param>
    /// <param name="byFragment">
    /// The <see cref="InlineFragmentNode"/> to <see cref="DeliveryGroup"/>
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
                    // Composite fields define the path for child leaves. Only
                    // leaf fields contribute to delivery group sets.
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
                    // Leaf fields contribute at the current path. Sibling
                    // @defer fragments that share a leaf are unified into a
                    // single incremental plan.
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
                    // An unlabeled nested @defer with the same condition as
                    // its parent shares the parent's delivery group when
                    // inlining is enabled.
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
