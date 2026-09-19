using System.Text.RegularExpressions;

namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Validates <c>agent_sessions.endpoint_addr</c> values against the grammar
/// enforced on write. A value that fails this grammar is demoted to
/// <c>endpoint_kind = 'none'</c> instead of being stored.
/// </summary>
internal static partial class EndpointAddress
{
    public static bool IsValid(string value) => Pattern().IsMatch(value);

    public static bool IsValidOpencodeServerUrl(string value)
        => Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    /// <summary>
    /// True only when <paramref name="value"/> is a syntactically valid
    /// opencode server URL and <paramref name="serverBound"/> confirms this
    /// process actually bound an HTTP server, not merely a placeholder URL.
    /// </summary>
    public static bool IsTrustedOpencodeServerUrl(string value, bool serverBound)
        => serverBound && IsValidOpencodeServerUrl(value);

    [GeneratedRegex(@"^[A-Za-z0-9._-]{1,128}$")]
    private static partial Regex Pattern();
}
