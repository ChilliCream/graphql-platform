using System.Security.Claims;
using HotChocolate.Features;

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
    /// Gets the user of the request that triggered the evaluation.
    /// </summary>
    ClaimsPrincipal User { get; }

    /// <summary>
    /// Gets the guarded resource the policy evaluates, or <c>null</c> when the evaluation is
    /// request-constant.
    /// </summary>
    /// <remarks>
    /// When <c>null</c>, this evaluation produces a single, request-constant decision: deny it,
    /// if at all, through <c>Deny(0, …)</c>. When non-<c>null</c>, evaluate every entity in
    /// <see cref="PolicySelection.Entities"/> and deny entities individually by their position in
    /// that batch.
    /// </remarks>
    PolicySelection? Selection { get; }

    /// <summary>
    /// Denies access to a single entity.
    /// </summary>
    /// <param name="index">
    /// The position of the denied entity within <see cref="PolicySelection.Entities"/>, or
    /// <c>0</c> when <see cref="Selection"/> is <c>null</c>.
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
