using System.Collections.Immutable;
using System.Security.Claims;

namespace HotChocolate.Fusion.Policies.Audit;

/// <summary>
/// The principal of a request captured as values.
/// </summary>
/// <param name="IsAuthenticated">
/// Whether the principal is authenticated.
/// </param>
/// <param name="Name">
/// The name of the primary identity, or <c>null</c> if it has none.
/// </param>
/// <param name="AuthenticationType">
/// The authentication type of the primary identity, or <c>null</c> if it has none.
/// </param>
/// <param name="Claims">
/// The claims of all identities of the principal.
/// </param>
public readonly record struct AuditSubject(
    bool IsAuthenticated,
    string? Name,
    string? AuthenticationType,
    ImmutableArray<AuditClaim> Claims)
{
    /// <summary>
    /// Captures the principal as values.
    /// </summary>
    /// <param name="user">
    /// The principal to capture.
    /// </param>
    public static AuditSubject Capture(ClaimsPrincipal user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var claims = ImmutableArray.CreateBuilder<AuditClaim>();

        foreach (var claim in user.Claims)
        {
            claims.Add(new AuditClaim(claim.Type, claim.Value));
        }

        return new AuditSubject(
            user.Identity?.IsAuthenticated is true,
            user.Identity?.Name,
            user.Identity?.AuthenticationType,
            claims.ToImmutable());
    }
}
