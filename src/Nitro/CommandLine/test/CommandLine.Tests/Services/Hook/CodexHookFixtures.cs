using System.Runtime.CompilerServices;

namespace ChilliCream.Nitro.CommandLine.Tests.Hook;

/// <summary>
/// Locates the Codex hook payload fixtures under
/// <c>test/fixtures/hooks/codex/</c>, sibling to the <c>CommandLine.Tests</c> project
/// directory. Mirrors <see cref="HookFixtures"/>'s resolution-by-source-path.
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
/// Locates the golden <c>hooks.json</c> "before" fixtures for
/// <c>hooks codex install/status/uninstall</c> under
/// <c>test/fixtures/hooks/codex/install/</c> and
/// <c>test/fixtures/hooks/codex/uninstall/</c>, and the <c>config.toml</c>
/// notify fixtures under <c>test/fixtures/hooks/codex/config-toml/</c>.
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
