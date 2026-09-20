namespace ChilliCream.Nitro.CommandLine;

internal static class AgentModeDetector
{
    private static readonly string[] s_environmentVariables =
    [
        "CLAUDECODE",
        "CLAUDE_CODE",
        "CODEX",
        "CURSOR_AGENT",
        "GITHUB_COPILOT",
        "CLINE",
        "WINDSURF_AGENT",
        "AIDER"
    ];

    public static bool IsEnabled(
        bool isOutputRedirected,
        Func<string, string?> getEnvironmentVariable)
    {
        ArgumentNullException.ThrowIfNull(getEnvironmentVariable);

        return isOutputRedirected
            || s_environmentVariables.Any(name => getEnvironmentVariable(name) is not null);
    }
}
