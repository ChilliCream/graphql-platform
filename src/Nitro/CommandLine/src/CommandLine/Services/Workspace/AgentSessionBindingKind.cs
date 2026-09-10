namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// The <c>agent_sessions.binding_kind</c> values, matching the table's CHECK
/// constraint.
/// </summary>
internal static class AgentSessionBindingKind
{
    public const string None = "none";
    public const string Env = "env";
    public const string Explicit = "explicit";
}
