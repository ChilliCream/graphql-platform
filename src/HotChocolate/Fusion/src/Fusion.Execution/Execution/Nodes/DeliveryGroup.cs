using HotChocolate.Execution;

namespace HotChocolate.Fusion.Execution.Nodes;

/// <summary>
/// Represents one delivery group introduced by a <c>@defer</c> directive.
/// Delivery groups form a parent chain for nested deferred fragments and are
/// referenced by selections when computing active delivery group sets.
/// </summary>
/// <param name="Label">
/// The optional label from <c>@defer(label: "...")</c>.
/// </param>
/// <param name="Parent">
/// The enclosing delivery group when this <c>@defer</c> is nested inside
/// another deferred fragment, or <c>null</c> for a top-level defer.
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
    /// A plan-stable numeric identifier for this delivery group.
    /// </summary>
    public int Id { get; init; } = -1;

    /// <summary>
    /// The selection path to the object whose selection set contains this
    /// <c>@defer</c>.
    /// </summary>
    public SelectionPath? Path { get; init; }

    /// <summary>
    /// The variable name from <c>@defer(if: $var)</c>, or <c>null</c> when this
    /// defer is unconditional. Runtime activation of this defer uses this variable
    /// together with <see cref="DeferConditionIndex"/>.
    /// </summary>
    public string? IfVariable { get; init; }
}
