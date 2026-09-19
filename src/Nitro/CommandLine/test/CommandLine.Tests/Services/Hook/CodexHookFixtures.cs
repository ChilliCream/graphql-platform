using System.Runtime.CompilerServices;

namespace ChilliCream.Nitro.CommandLine.Tests.Hook;

/// <summary>
/// Reads captured Codex hook payload fixtures from <c>test/fixtures/hooks/codex/</c>, resolved
/// relative to this file's own source path so it works regardless of the test run's working
/// directory.
/// </summary>
internal static class CodexHookFixtures
{
    public static string Read(string fileName, [CallerFilePath] string sourceFile = "")
    {
        var directory = Path.GetDirectoryName(sourceFile)!;
        var path = Path.Combine(directory, "..", "..", "..", "fixtures", "hooks", "codex", fileName);

        return File.ReadAllText(path);
    }
}

/// <summary>
/// Reads golden <c>hooks.json</c> "before" fixtures for <c>hooks codex install/status/uninstall</c>
/// and <c>config.toml</c> notify fixtures from <c>test/fixtures/hooks/codex/&lt;category&gt;/</c>.
/// </summary>
internal static class CodexHooksInstallFixtures
{
    public static string Read(string category, string fileName, [CallerFilePath] string sourceFile = "")
    {
        var directory = Path.GetDirectoryName(sourceFile)!;
        var path = Path.Combine(directory, "..", "..", "..", "fixtures", "hooks", "codex", category, fileName);

        return File.ReadAllText(path);
    }
}
