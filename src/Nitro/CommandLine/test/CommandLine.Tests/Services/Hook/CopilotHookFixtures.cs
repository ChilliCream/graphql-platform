using System.Runtime.CompilerServices;

namespace ChilliCream.Nitro.CommandLine.Tests.Hook;

/// <summary>
/// Locates the captured Copilot hook payload fixtures under
/// <c>test/fixtures/hooks/copilot/redo-1.0.80/</c> (spike S5's redo,
/// perles-net-k3j.4, against the actually-running 1.0.80 binary), sibling to
/// the <c>CommandLine.Tests</c> project directory. Mirrors
/// <see cref="CodexHookFixtures"/>'s resolution-by-source-path.
/// </summary>
internal static class CopilotHookFixtures
{
    public static string Read(string fileName, [CallerFilePath] string sourceFile = "")
    {
        var directory = Path.GetDirectoryName(sourceFile)!;
        var path = Path.Combine(
            directory, "..", "..", "..", "fixtures", "hooks", "copilot", "redo-1.0.80", fileName);

        return File.ReadAllText(path);
    }
}

/// <summary>
/// Locates the golden hooks-dir "before" fixtures for
/// <c>hooks copilot install/status/uninstall</c> under
/// <c>test/fixtures/hooks/copilot/install/</c> and
/// <c>test/fixtures/hooks/copilot/uninstall/</c>.
/// </summary>
internal static class CopilotHooksInstallFixtures
{
    public static string Read(string category, string fileName, [CallerFilePath] string sourceFile = "")
    {
        var directory = Path.GetDirectoryName(sourceFile)!;
        var path = Path.Combine(directory, "..", "..", "..", "fixtures", "hooks", "copilot", category, fileName);

        return File.ReadAllText(path);
    }
}
