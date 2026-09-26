using System.Collections.Frozen;

namespace ChilliCream.Nitro.CommandLine;

internal static class AgentModeDetector
{
    private static readonly FrozenSet<string> s_environmentVariables =
        new[]
        {
            "CLAUDECODE",
            "CLAUDE_CODE",
            "CODEX",
            "CURSOR_AGENT",
            "GITHUB_COPILOT",
            "CLINE",
            "WINDSURF_AGENT",
            "AIDER"
        }.ToFrozenSet(StringComparer.Ordinal);

    public static bool IsEnabled(
        bool isOutputRedirected,
        Func<string, string?> getEnvironmentVariable)
    {
        ArgumentNullException.ThrowIfNull(getEnvironmentVariable);

        return isOutputRedirected
            || s_environmentVariables.Any(name => getEnvironmentVariable(name) is not null);
    }
}
