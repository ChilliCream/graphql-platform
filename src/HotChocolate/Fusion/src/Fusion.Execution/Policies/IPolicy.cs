using HotChocolate.Fusion.Text.Json;
using HotChocolate.Language;

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
    /// Gets the graph data required to evaluate this policy.
    /// </summary>
    /// <remarks>
    /// When this property is <c>null</c>, the policy produces a target-independent decision.
    /// The policy is evaluated lazily at most once per request, and its decision is reused for
    /// every application of the policy that is reached during that request.
    /// Such a policy must not derive its decision from target-specific context or entity data.
    /// </remarks>
    SelectionSetNode? Requirements { get; }

    ValueTask EvaluateAsync(
        IPolicyContext context,
        ReadOnlyMemory<CompositeResultElement> entities,
        CancellationToken cancellationToken = default);
}
