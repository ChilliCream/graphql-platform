using HotChocolate.Execution;

namespace HotChocolate.Fusion.Execution.Nodes;

/// <summary>
/// Represents a usage of the <c>@defer</c> directive. This is the plan-level identity
/// of a single deferred fragment (what the GraphQL Incremental Delivery spec calls a
/// "delivery group"). One instance per <c>@defer</c> occurrence in an operation;
/// instances form a parent chain to model nested defer scopes and are referenced
/// by identity from <see cref="Selection"/>'s active defer-usage set.
/// </summary>
/// <param name="Label">
/// The optional label from <c>@defer(label: "...")</c>, used to identify the deferred
/// payload in the incremental delivery response.
/// </param>
/// <param name="Parent">
/// The parent defer usage when this <c>@defer</c> is nested inside another deferred fragment,
/// or <c>null</c> if this is a top-level defer.
/// </param>
/// <param name="DeferConditionIndex">
/// The index into the <see cref="DeferConditionCollection"/> for the <c>if</c> condition
/// associated with this defer directive. This index maps to a bit position in the
/// runtime defer flags bitmask.
/// </param>
public sealed record DeliveryGroup(
    string? Label,
    DeliveryGroup? Parent,
    byte DeferConditionIndex)
{
    /// <summary>
    /// A plan-stable numeric identifier for this defer usage, assigned when the
    /// <see cref="OperationPlan"/> is built. Serialized as the delivery group's
    /// identity and used as the <c>id</c> in <c>pending</c>, <c>incremental</c>,
    /// and <c>completed</c> entries of the incremental delivery response.
    /// </summary>
    public int Id { get; init; } = -1;

    /// <summary>
    /// The selection path to the object in the response tree whose selection set
    /// contains this <c>@defer</c>. Used as the <c>path</c> of the corresponding
    /// <c>pending</c> entry on the wire.
    /// </summary>
    public SelectionPath? Path { get; init; }

    /// <summary>
    /// The variable name from <c>@defer(if: $var)</c>, or <c>null</c> when this
    /// defer is unconditional. Runtime activation of this defer uses this variable
    /// together with <see cref="DeferConditionIndex"/>.
    /// </summary>
    public string? IfVariable { get; init; }
}
