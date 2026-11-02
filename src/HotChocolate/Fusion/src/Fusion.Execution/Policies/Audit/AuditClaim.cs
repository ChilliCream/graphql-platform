namespace HotChocolate.Fusion.Policies.Audit;

/// <summary>
/// A claim of the audited principal.
/// </summary>
/// <param name="Type">
/// The claim type.
/// </param>
/// <param name="Value">
/// The claim value.
/// </param>
public readonly record struct AuditClaim(string Type, string Value);
