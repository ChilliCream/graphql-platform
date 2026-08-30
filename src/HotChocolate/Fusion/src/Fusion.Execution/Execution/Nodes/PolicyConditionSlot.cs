using System.Collections.Immutable;
using HotChocolate.Fusion.Types;

namespace HotChocolate.Fusion.Execution.Nodes;

/// <summary>
/// Describes one plan-time policy condition slot.
/// </summary>
/// <remarks>
/// A slot represents a policy expression built entirely from request-cacheable policy names
/// (see <see cref="PolicyRequirements.IsRequestCacheable"/>), canonicalized so that two
/// applications with the same names and groups, regardless of order, share one slot. A slot is
/// intended to be evaluated through the same boolean condition machinery used for <c>@skip</c>
/// and <c>@include</c> (see <see cref="ExecutionNodeCondition"/>), so that a coordinate whose
/// applications are entirely covered by slots eventually needs no dedicated policy execution
/// node. Attaching the resulting <see cref="ExecutionNodeCondition"/> to a plan step and, once
/// every application on a coordinate is slot-covered, dropping that coordinate's policy
/// execution target are both request-time overlay concerns, not implemented here.
/// </remarks>
public sealed record PolicyConditionSlot
{
    /// <summary>
    /// Gets the zero-based, plan-local ordinal of this slot.
    /// </summary>
    public required int Ordinal { get; init; }

    /// <summary>
    /// Gets the canonicalized policy name groups that form the slot's expression. Names within
    /// a group combine with AND, groups combine with OR.
    /// </summary>
    public required ImmutableArray<ImmutableArray<string>> Groups { get; init; }

    /// <summary>
    /// Gets the residual denial threshold for this slot: the most severe
    /// <see cref="PolicyDenialBehavior"/> that applies to any policy application covered by
    /// this slot's coordinate.
    /// </summary>
    public required PolicyDenialBehavior Rmax { get; init; }

    /// <summary>
    /// Gets the name of the boolean variable that carries this slot's outcome, in the reserved
    /// <c>__fusion_policy</c> namespace.
    /// </summary>
    public string VariableName => $"__fusion_policy_{Ordinal}";

    /// <summary>
    /// Formats the slot's policy expression for diagnostics, for example (a AND b) OR c.
    /// </summary>
    public string Format() => global::HotChocolate.Fusion.PolicyNameGroups.Format(Groups);
}
