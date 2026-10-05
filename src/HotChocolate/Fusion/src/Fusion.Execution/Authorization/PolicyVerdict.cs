using System.Collections.Immutable;

namespace HotChocolate.Fusion.Authorization;

/// <summary>
/// The result a policy recorded for a single <see cref="PolicyEvaluationEntry"/>.
/// </summary>
/// <param name="Outcome">
/// The decision of the policy.
/// </param>
/// <param name="Reason">
/// An optional reason for the decision that is only meant for auditing.
/// </param>
/// <param name="AuditData">
/// Optional key/value pairs that are only meant for auditing.
/// </param>
public readonly record struct PolicyVerdict(
    PolicyOutcome Outcome,
    string? Reason,
    ImmutableDictionary<string, string>? AuditData);
