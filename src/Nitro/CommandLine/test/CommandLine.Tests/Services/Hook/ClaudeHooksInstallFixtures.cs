using System.Runtime.CompilerServices;

namespace ChilliCream.Nitro.CommandLine.Tests.Hook;

/// <summary>
/// Locates the golden <c>settings.json</c> "before" fixtures for
/// <c>hooks install/status/uninstall</c> under
/// <c>test/fixtures/hooks/claude/install/</c> and
/// <c>test/fixtures/hooks/claude/uninstall/</c>, sibling to the
/// <c>CommandLine.Tests</c> project directory. Mirrors
/// <c>HookFixtures</c>'s resolution-by-source-path so tests work regardless
/// of the run's working directory.
/// </summary>
internal static class ClaudeHooksInstallFixtures
{
    public static string Read(string category, string fileName, [CallerFilePath] string sourceFile = "")
    {
        var directory = Path.GetDirectoryName(sourceFile)!;
        var path = Path.Combine(directory, "..", "..", "..", "fixtures", "hooks", "claude", category, fileName);

        return File.ReadAllText(path);
    }
}
