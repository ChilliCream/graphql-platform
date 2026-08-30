using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution;

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
    /// Gets whether the policy produces a request-constant decision that may be evaluated at most once
    /// per request and reused for every application reached during that request.
    /// </summary>
    /// <remarks>
    /// A policy is request cacheable when it reads no resource.
    /// </remarks>
    public bool IsRequestCacheable => Resource is null;
}
