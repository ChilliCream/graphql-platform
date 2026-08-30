namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Normalizes agent role values used throughout the agent registry.
/// </summary>
internal static class AgentRole
{
    /// <summary>
    /// Trims and lowercases the given value. A null or whitespace-only value
    /// normalizes to the empty string; unlike an agent name, a role may be
    /// empty and carries no character restriction.
    /// </summary>
    public static string Normalize(string? role) => (role ?? string.Empty).Trim().ToLowerInvariant();
}
