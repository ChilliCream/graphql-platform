namespace HotChocolate.Fusion.Execution;

/// <summary>
/// Represents an authorization policy owned by a Fusion schema.
/// </summary>
/// <remarks>
/// <see cref="Name"/> and <see cref="Requirements"/> must remain stable for the schema lifetime.
/// <see cref="EvaluateAsync"/> may be called concurrently.
/// </remarks>
public interface IPolicy
{
    string Name { get; }

    /// <summary>
    /// Gets the parts of the evaluation input this policy reads.
    /// </summary>
    /// <remarks>
    /// When <see cref="PolicyRequirements.IsRequestCacheable"/> is <c>true</c>, the policy produces a
    /// request-constant decision that is evaluated at most once per request and reused for every
    /// application reached during that request. Such a policy must not derive its decision from
    /// target-specific context or entity data.
    /// </remarks>
    PolicyRequirements Requirements { get; }

    /// <summary>
    /// Evaluates this policy against the given context.
    /// </summary>
    /// <remarks>
    /// When <paramref name="context"/>'s <see cref="IPolicyContext.Selection"/> is <c>null</c>, this
    /// call produces a single request-constant decision and denies it, if at all, through
    /// <c>context.Deny(0, …)</c>. Otherwise, the policy evaluates every entity in
    /// <see cref="PolicySelection.Entities"/> and denies entities individually by their position in
    /// that batch.
    /// </remarks>
    ValueTask EvaluateAsync(
        IPolicyContext context,
        CancellationToken cancellationToken);
}
