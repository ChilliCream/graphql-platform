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

    [GeneratedRegex(@"^[A-Za-z0-9._-]{1,128}$")]
    private static partial Regex Pattern();
}
