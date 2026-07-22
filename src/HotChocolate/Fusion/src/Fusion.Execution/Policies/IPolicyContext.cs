using System.Security.Claims;
using HotChocolate.Execution;
using HotChocolate.Features;
using HotChocolate.Fusion.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution;

/// <summary>
/// Represents the context of a single policy evaluation and receives the decisions
/// the policy makes for the entities it evaluates.
/// </summary>
/// <remarks>
/// A context is only valid for the duration of the
/// <see cref="IPolicy.EvaluateAsync"/> call it is passed to and must not be used
/// after the call has completed.
/// </remarks>
public interface IPolicyContext : IFeatureProvider
{
    /// <summary>
    /// Gets the type definition the policy application targets.
    /// </summary>
    ITypeDefinition Type { get; }

    /// <summary>
    /// Gets the selection the policy application guards, or <c>null</c> when the policy
    /// guards a type definition rather than a selection.
    /// </summary>
    ISelection? Selection { get; }

    /// <summary>
    /// Gets the consequence that applies to the values this evaluation denies.
    /// </summary>
    /// <remarks>
    /// The executor applies the consequence after the evaluation completes. A policy
    /// only reports denials through <see cref="Deny"/> and does not act on this value.
    /// </remarks>
    PolicyDenialBehavior OnDenied { get; }

    /// <summary>
    /// Gets the user of the request that triggered the evaluation.
    /// </summary>
    ClaimsPrincipal User { get; }

    /// <summary>
    /// Denies access to a single entity.
    /// </summary>
    /// <param name="index">
    /// The position of the denied entity within the entities passed to
    /// <see cref="IPolicy.EvaluateAsync"/>.
    /// </param>
    /// <param name="reason">
    /// An optional human readable explanation that is surfaced with the resulting
    /// authorization error.
    /// </param>
    /// <remarks>
    /// Entities that are not denied during the evaluation are allowed. Denying the
    /// same entity more than once is permitted and replaces the previously recorded
    /// reason.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="index"/> is negative or not less than the number of evaluated
    /// entities.
    /// </exception>
    void Deny(int index, string? reason = null);
}
