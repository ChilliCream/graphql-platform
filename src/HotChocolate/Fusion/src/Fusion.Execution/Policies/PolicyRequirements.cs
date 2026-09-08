using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution;

/// <summary>
/// Distinguishes the two evaluation shapes a requirement-free policy (one with no
/// <see cref="PolicyRequirements.Resource"/>) can have.
/// </summary>
public enum PolicyEvaluationKind
{
    /// <summary>
    /// The policy reads neither a resource nor an action and produces a single decision that is
    /// evaluated at most once per request and reused for every application reached during that
    /// request.
    /// </summary>
    RequestConstant,

    /// <summary>
    /// The policy reads the guarded field's name and its coerced arguments
    /// (<see cref="IPolicyContext.Action"/>) and is evaluated once per compiled selection
    /// occurrence, using that occurrence's own arguments; its decision is never reused across
    /// occurrences or cached across a variable batch.
    /// </summary>
    ActionOccurrence
}

/// <summary>
/// Describes the graph data a policy declares it reads.
/// </summary>
public sealed class PolicyRequirements
{
    /// <summary>
    /// The requirements of a policy that reads no input part. Such a policy produces a
    /// request-constant decision.
    /// </summary>
    public static PolicyRequirements Empty { get; } = new();

    /// <summary>
    /// Gets the selection set over the guarded graph types that projects the resource part of the
    /// evaluation input, or <c>null</c> when the policy reads no resource.
    /// </summary>
    /// <remarks>
    /// A resource-bearing policy is evaluated per entity batch and never reuses a decision across
    /// entities.
    /// </remarks>
    public SelectionSetNode? Resource { get; init; }

    /// <summary>
    /// Gets the evaluation shape of a requirement-free policy (one with no <see cref="Resource"/>).
    /// Meaningless when <see cref="Resource"/> is set. Defaults to
    /// <see cref="PolicyEvaluationKind.RequestConstant"/>.
    /// </summary>
    public PolicyEvaluationKind Kind { get; init; } = PolicyEvaluationKind.RequestConstant;

    /// <summary>
    /// Gets whether the policy produces a request-constant decision that may be evaluated at most once
    /// per request and reused for every application reached during that request.
    /// </summary>
    /// <remarks>
    /// A policy is request cacheable when it reads no resource and no action.
    /// </remarks>
    public bool IsRequestCacheable => Resource is null && Kind == PolicyEvaluationKind.RequestConstant;
}
