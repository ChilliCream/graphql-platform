namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Generates the deterministic, harness-namespaced actor name a SessionStart
/// binds a session to when no explicit actor is configured (<see
/// cref="Mail.MailActor.TryResolve"/> returns null): the harness's short
/// prefix, a hyphen, then the session id with every character <see
/// cref="Mail.MailAgentName.Normalize"/> would reject replaced by a hyphen,
/// so the result always satisfies actor validation regardless of the session
/// id's own charset. Stable for the same (harness, session id) pair across
/// duplicate SessionStart calls, and unique across distinct harness or
/// session id values, since the harness prefix and the id are both present.
/// </summary>
internal static class AgentSessionActorNaming
{
    public static string Generate(string harness, string sessionId)
    {
        var prefix = harness switch
        {
            AgentSessionHarness.ClaudeCode => "claude",
            AgentSessionHarness.Codex => "codex",
            AgentSessionHarness.Copilot => "copilot",
            _ => harness
        };

        return $"{prefix}-{Sanitize(sessionId)}";
    }

    private static string Sanitize(string sessionId)
    {
        var lowered = sessionId.ToLowerInvariant();
        var chars = lowered.ToCharArray();

        for (var i = 0; i < chars.Length; i++)
        {
            var c = chars[i];

            if (c is not (>= 'a' and <= 'z' or >= '0' and <= '9' or '-' or '_'))
            {
                chars[i] = '-';
            }
        }

        return new string(chars);
    }
}
