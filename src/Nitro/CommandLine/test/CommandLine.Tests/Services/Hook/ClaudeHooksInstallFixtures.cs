using System.Runtime.CompilerServices;

namespace ChilliCream.Nitro.CommandLine.Tests.Hook;

/// <summary>
/// Reads golden <c>settings.json</c> "before" fixtures for <c>hooks install/status/uninstall</c>
/// from <c>test/fixtures/hooks/claude/&lt;category&gt;/</c>, resolved relative to this file's
/// own source path so it works regardless of the test run's working directory.
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
