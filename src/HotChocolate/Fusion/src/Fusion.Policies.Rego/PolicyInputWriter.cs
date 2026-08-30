using System.Security.Claims;
using HotChocolate.Text.Json;

namespace HotChocolate.Fusion.Policies.Rego;

internal static class PolicyInputWriter
{
    public static void WriteDefaultSubject(JsonWriter writer, ClaimsPrincipal user)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("id");
        writer.WriteStringValue(
            user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.Identity?.Name);

        writer.WritePropertyName("roles");
        writer.WriteStartArray();

        foreach (var claim in user.Claims)
        {
            if (IsRoleClaim(claim))
            {
                writer.WriteStringValue(claim.Value);
            }
        }

        writer.WriteEndArray();

        writer.WritePropertyName("claims");
        writer.WriteStartObject();

        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var claim in user.Claims)
        {
            if (IsRoleClaim(claim) || !seen.Add(claim.Type))
            {
                continue;
            }

            writer.WritePropertyName(claim.Type);
            writer.WriteStringValue(claim.Value);
        }

        writer.WriteEndObject();
        writer.WriteEndObject();
    }

    private static bool IsRoleClaim(Claim claim)
        => string.Equals(
            claim.Type,
            claim.Subject?.RoleClaimType ?? ClaimsIdentity.DefaultRoleClaimType,
            StringComparison.Ordinal);
}
