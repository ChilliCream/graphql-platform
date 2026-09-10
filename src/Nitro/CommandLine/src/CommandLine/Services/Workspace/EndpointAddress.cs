using System.Text.RegularExpressions;

namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Validates <c>agent_sessions.endpoint_addr</c> values against the grammar
/// enforced on write: harness-derived peer names and thread/session ids are
/// not <see cref="Mail.MailAgentName"/>-validated at the source, so a value
/// that fails this grammar is demoted to <c>endpoint_kind = 'none'</c>
/// instead of being stored.
/// </summary>
internal static partial class EndpointAddress
{
    public static bool IsValid(string value) => Pattern().IsMatch(value);

    public static bool IsValidOpencodeServerUrl(string value)
        => Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    /// <summary>
    /// True only when <paramref name="value"/> is a syntactically valid
    /// opencode server URL AND the shim reported that this process passed
    /// one of the flags that actually make opencode bind an HTTP server
    /// (<c>--port</c>, <c>--hostname</c>, or <c>--mdns</c>). A plain
    /// <c>opencode</c> TUI binds none of them: it reaches its own server
    /// inside a Worker over postMessage RPC, and the plugin's
    /// <c>serverUrl</c> getter then falls back to a hardcoded
    /// <c>http://localhost:4096</c> placeholder that is syntactically fine
    /// but proves nothing about what, if anything, is listening there. That
    /// is worse than a dead port: 4096 is also opencode's own
    /// <c>opencode serve</c> default, so trusting the placeholder risks
    /// pushing into an unrelated process's session (see hc-10-w61.1).
    /// <paramref name="serverBound"/> is the shim's own answer, taken from
    /// its process.argv, to the one question that actually decides whether
    /// opencode bound a server - opencode has no other process-identity
    /// probe this hook can call to confirm the endpoint belongs to it.
    /// </summary>
    public static bool IsTrustedOpencodeServerUrl(string value, bool serverBound)
        => serverBound && IsValidOpencodeServerUrl(value);

    [GeneratedRegex(@"^[A-Za-z0-9._-]{1,128}$")]
    private static partial Regex Pattern();
}
