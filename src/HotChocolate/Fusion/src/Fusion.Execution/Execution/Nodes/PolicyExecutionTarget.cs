using HotChocolate.Execution;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Nodes;

/// <summary>
/// Describes one policy application target in an operation plan.
/// </summary>
public sealed record PolicyExecutionTarget
{
    /// <summary>
    /// Gets whether the target is an object or field result position.
    /// </summary>
    public required PolicyTargetKind Kind { get; init; }

    /// <summary>
    /// Gets the response path where the policy effect applies.
    /// </summary>
    public required SelectionPath Path { get; init; }

    /// <summary>
    /// Gets the composite type name that owns the policy coordinate.
    /// </summary>
    public required string TypeName { get; init; }

    /// <summary>
    /// Gets the composite field name when <see cref="Kind"/> is <see cref="PolicyTargetKind.Field"/>.
    /// </summary>
    public string? FieldName { get; init; }

    /// <summary>
    /// Gets the policy applications to evaluate for this target.
    /// </summary>
    public required PolicyApplication[] Policies { get; init; }

    /// <summary>
    /// Gets the immutable policy requirements used to build this operation plan.
    /// </summary>
    public AuthorizationPolicyRequirement[] Requirements { get; init; } = [];

    /// <summary>
    /// Gets the conditions that control whether this target is active.
    /// </summary>
    public ExecutionNodeCondition[] Conditions { get; init; } = [];
}

/// <summary>
/// Captures the requirements of one policy in an operation plan.
/// </summary>
public sealed record AuthorizationPolicyRequirement
{
    public required string PolicyName { get; init; }

    public required SelectionSetNode SelectionSet { get; init; }
}
