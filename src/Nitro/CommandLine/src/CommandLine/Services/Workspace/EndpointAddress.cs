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

    [GeneratedRegex(@"^[A-Za-z0-9._-]{1,128}$")]
    private static partial Regex Pattern();
}
