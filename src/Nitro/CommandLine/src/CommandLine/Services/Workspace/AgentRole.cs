namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Defines the well-known agent roles and normalizes role values used throughout the agent registry.
/// Custom normalized roles are also accepted.
/// </summary>
internal static class AgentRole
{
    public const string Orchestrator = "orchestrator";
    public const string Planner = "planner";
    public const string Implementer = "implementer";
    public const string Reviewer = "reviewer";
    public const string Researcher = "researcher";

    public static IReadOnlyList<string> WellKnown { get; } = Array.AsReadOnly<string>(
    [
        Orchestrator,
        Planner,
        Implementer,
        Reviewer,
        Researcher
    ]);

    /// <summary>
    /// Trims and lowercases the given value. A null or whitespace-only value
    /// normalizes to the empty string; unlike an agent name, a role may be
    /// empty and carries no character restriction.
    /// </summary>
    public static string Normalize(string? role) => (role ?? string.Empty).Trim().ToLowerInvariant();
}
