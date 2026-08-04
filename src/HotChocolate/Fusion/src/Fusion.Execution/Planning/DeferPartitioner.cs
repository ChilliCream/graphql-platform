using System.Collections.Immutable;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Planning;

/// <summary>
/// Walks an operation AST once and produces the <see cref="DeliveryGroup"/> tree
/// for all <c>@defer</c> occurrences. The resulting mapping from
/// <see cref="InlineFragmentNode"/> instance to <see cref="DeliveryGroup"/> is the
/// single source of truth for defer topology, consumed by the planner pipeline
/// stages (rewriter, compiler) so they do not perform parallel AST walks with
/// divergent state.
/// </summary>
internal static class DeferPartitioner
{
    /// <summary>
    /// Walks <paramref name="operation"/> and produces the complete
    /// <see cref="DeliveryGroup"/> tree for every <c>... @defer</c> inline
    /// fragment encountered. Defer conditions are registered into
    /// <paramref name="deferConditions"/> (passed in rather than owned so the
    /// caller can share one collection with the operation being compiled).
    /// </summary>
    public static DeferPartitioningResult Partition(
        OperationDefinitionNode operation,
        DeferConditionCollection deferConditions)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(deferConditions);

        var byFragment = new Dictionary<InlineFragmentNode, DeliveryGroup>(ReferenceEqualityComparer.Instance);
        var ordered = new List<DeliveryGroup>();

        Walk(
            operation.SelectionSet.Selections,
            parent: null,
            currentPath: SelectionPath.Root,
            deferConditions,
            byFragment,
            ordered);

        // Assign plan-stable Ids in declaration order and update parent
        // references to point to the Id-assigned records.
        var reassigned = AssignIds(ordered, byFragment);

        return new DeferPartitioningResult(reassigned, byFragment);
    }

    private static void Walk(
        IReadOnlyList<ISelectionNode> selections,
        DeliveryGroup? parent,
        SelectionPath currentPath,
        DeferConditionCollection deferConditions,
        Dictionary<InlineFragmentNode, DeliveryGroup> byFragment,
        List<DeliveryGroup> ordered)
    {
        for (var i = 0; i < selections.Count; i++)
        {
            var selection = selections[i];

            if (selection is FieldNode field)
            {
                if (field.SelectionSet is { } sub)
                {
                    var responseName = field.Alias?.Value ?? field.Name.Value;
                    var childPath = currentPath.AppendField(responseName);
                    Walk(sub.Selections, parent, childPath, deferConditions, byFragment, ordered);
                }

                continue;
            }

            if (selection is InlineFragmentNode inline)
            {
                var nested = parent;

                if (DeferCondition.TryCreate(inline, out var deferCondition))
                {
                    deferConditions.Add(deferCondition);
                    var deferIndex = deferConditions.IndexOf(deferCondition);
                    var label = GetDeferLabel(inline);
                    var ifVariable = GetDeferIfVariable(inline);

                    var usage = new DeliveryGroup(label, parent, (byte)deferIndex)
                    {
                        Path = currentPath,
                        IfVariable = ifVariable
                    };
                    byFragment[inline] = usage;
                    ordered.Add(usage);
                    nested = usage;
                }

                Walk(inline.SelectionSet.Selections, nested, currentPath, deferConditions, byFragment, ordered);
                continue;
            }

            // FragmentSpreadNode / other shapes are not expected at planner input
            // (document rewriter inlines spreads); fall through silently.
        }
    }

    /// <summary>
    /// Assigns plan-stable Ids to every <see cref="DeliveryGroup"/> in declaration
    /// order and rebuilds parent references against the Id-assigned instances.
    /// The <paramref name="byFragment"/> map is updated in place so callers see
    /// the canonical records.
    /// </summary>
    private static ImmutableArray<DeliveryGroup> AssignIds(
        List<DeliveryGroup> ordered,
        Dictionary<InlineFragmentNode, DeliveryGroup> byFragment)
    {
        if (ordered.Count == 0)
        {
            return [];
        }

        var remap = new Dictionary<DeliveryGroup, DeliveryGroup>(ordered.Count, ReferenceEqualityComparer.Instance);
        var builder = ImmutableArray.CreateBuilder<DeliveryGroup>(ordered.Count);

        for (var i = 0; i < ordered.Count; i++)
        {
            var source = ordered[i];
            var parent = source.Parent is null ? null : remap[source.Parent];
            var reassigned = source with { Id = i, Parent = parent };
            remap[source] = reassigned;
            builder.Add(reassigned);
        }

        // Keep the fragment lookup in sync with the Id-assigned records.
        foreach (var kvp in byFragment.ToArray())
        {
            byFragment[kvp.Key] = remap[kvp.Value];
        }

        return builder.MoveToImmutable();
    }

    private static string? GetDeferLabel(InlineFragmentNode node)
    {
        for (var i = 0; i < node.Directives.Count; i++)
        {
            var directive = node.Directives[i];

            if (!directive.Name.Value.Equals(
                DirectiveNames.Defer.Name,
                StringComparison.Ordinal))
            {
                continue;
            }

            for (var j = 0; j < directive.Arguments.Count; j++)
            {
                var arg = directive.Arguments[j];

                if (arg.Name.Value.Equals("label", StringComparison.Ordinal)
                    && arg.Value is StringValueNode stringValue)
                {
                    return stringValue.Value;
                }
            }
        }

        return null;
    }

    private static string? GetDeferIfVariable(InlineFragmentNode node)
    {
        for (var i = 0; i < node.Directives.Count; i++)
        {
            var directive = node.Directives[i];

            if (!directive.Name.Value.Equals(
                DirectiveNames.Defer.Name,
                StringComparison.Ordinal))
            {
                continue;
            }

            for (var j = 0; j < directive.Arguments.Count; j++)
            {
                var arg = directive.Arguments[j];

                if (arg.Name.Value.Equals("if", StringComparison.Ordinal)
                    && arg.Value is VariableNode variable)
                {
                    return variable.Name.Value;
                }
            }
        }

        return null;
    }
}
