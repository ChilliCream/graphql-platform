namespace HotChocolate.Fusion.Execution.Nodes;

/// <summary>
/// Describes one authorization policy referenced by an operation plan, either through a plan-time
/// <see cref="PolicyConditionSlot"/> or through a policy execution node target.
/// </summary>
/// <remarks>
/// The operation plan carries one entry per distinct referenced policy name, so that a request can
/// resolve every policy instance it needs ahead of execution, through the same per-operation memo
/// <see cref="OperationPlanContext"/> uses elsewhere, and so that a change to what a policy requires
/// can be recognized against the plan that was built for the previous requirement.
/// </remarks>
public sealed record PolicyPlanEntry
{
    /// <summary>
    /// Gets the name of the referenced policy.
    /// </summary>
    public required string PolicyName { get; init; }

    /// <summary>
    /// Gets the ordinal of the <see cref="PolicyConditionSlot"/> this policy contributes to, or
    /// <c>null</c> when the policy is referenced through a policy execution node target instead of
    /// a slot.
    /// </summary>
    public int? Slot { get; init; }

    /// <summary>
    /// Gets the canonical expression of the owning slot, for diagnostics, or <c>null</c> when
    /// <see cref="Slot"/> is <c>null</c>.
    /// </summary>
    public string? Expression { get; init; }

    /// <summary>
    /// Gets a content hash of the resource requirement this plan was built against for this
    /// policy, or <c>0</c> when the plan was built assuming the policy has no resource
    /// requirement.
    /// </summary>
    public required ulong RequirementHash { get; init; }
}
