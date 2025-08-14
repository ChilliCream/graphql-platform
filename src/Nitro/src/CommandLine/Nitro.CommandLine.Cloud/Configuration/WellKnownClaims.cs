using Duende.IdentityModel;

namespace ChilliCream.Nitro.CLI;

internal static class WellKnownClaims
{
    public const string Tenant = "tenant";

    public const string ApiUrl = "api_url";

    public const string Email = JwtClaimTypes.Email;

    public const string UserId = JwtClaimTypes.Subject;

    public const string Issuer = JwtClaimTypes.Issuer;

    public const string SessionId = JwtClaimTypes.SessionId;
}
