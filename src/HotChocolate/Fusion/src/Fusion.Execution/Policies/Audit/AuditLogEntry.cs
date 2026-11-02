using System.Collections.Immutable;

namespace HotChocolate.Fusion.Policies.Audit;

/// <summary>
/// The input and the outcome of one policy evaluation for one directive occurrence.
/// </summary>
/// <param name="Coordinate">
/// The schema coordinate of the protected field.
/// </param>
/// <param name="DirectiveName">
/// The name of the authorization directive.
/// </param>
/// <param name="PolicyName">
/// The policy name, or <c>null</c> for directives without one.
/// </param>
/// <param name="Scopes">
/// The required scopes as OR-of-AND groups, which is empty for directives without scopes.
/// </param>
/// <param name="Arguments">
/// The arguments of the field as GraphQL literals, with variables replaced by their values.
/// </param>
/// <param name="Outcome">
/// The outcome of the evaluation.
/// </param>
/// <param name="Reason">
/// The reason of the outcome, or <c>null</c> if there is none.
/// </param>
/// <param name="AuditData">
/// The audit data of the policy, or <c>null</c> if it attached none.
/// </param>
public readonly record struct AuditLogEntry(
    SchemaCoordinate Coordinate,
    string DirectiveName,
    string? PolicyName,
    ImmutableArray<ImmutableArray<string>> Scopes,
    ImmutableDictionary<string, string> Arguments,
    PolicyOutcome Outcome,
    string? Reason,
    ImmutableDictionary<string, string>? AuditData);
