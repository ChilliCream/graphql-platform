using System.Text.RegularExpressions;

namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Validates session endpoint names and opencode server URLs.
/// </summary>
internal static partial class EndpointAddress
{
    public static bool IsValid(string value) => Pattern().IsMatch(value);

    public static bool IsValidOpencodeServerUrl(string value)
        => Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    /// <summary>
    /// Returns true when <paramref name="serverBound"/> is true and
    /// <paramref name="value"/> is an absolute HTTP or HTTPS URL.
    /// </summary>
    public static bool IsTrustedOpencodeServerUrl(string value, bool serverBound)
        => serverBound && IsValidOpencodeServerUrl(value);

    /// <summary>
    /// Normalizes invalid or absent endpoints to kind <c>none</c>, an empty address,
    /// and no credential. Credentials are retained only for valid opencode-server
    /// endpoints belonging to the opencode harness.
    /// </summary>
    public static (string Kind, string Addr, string? Secret) Normalize(
        string harness,
        string endpointKind,
        string endpointAddr,
        string? endpointSecret)
    {
        if (endpointKind == AgentSessionEndpointKind.OpencodeServer)
        {
            return IsValidOpencodeServerUrl(endpointAddr)
                ? (endpointKind, endpointAddr, harness == AgentSessionHarness.Opencode ? endpointSecret : null)
                : (AgentSessionEndpointKind.None, string.Empty, null);
        }

        if (endpointKind == AgentSessionEndpointKind.None || !IsValid(endpointAddr))
        {
            return (AgentSessionEndpointKind.None, string.Empty, null);
        }

        return (endpointKind, endpointAddr, null);
    }

    [GeneratedRegex(@"^[A-Za-z0-9._-]{1,128}$")]
    private static partial Regex Pattern();
}
