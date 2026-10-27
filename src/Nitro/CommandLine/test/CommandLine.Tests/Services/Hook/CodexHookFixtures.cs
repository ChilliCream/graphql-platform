using System.Runtime.CompilerServices;

namespace ChilliCream.Nitro.CommandLine.Tests.Hook;

/// <summary>
/// Reads captured Codex hook payload fixtures from
/// <c>test/fixtures/hooks/codex/</c>, relative to the calling source file.
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
/// Reads Codex hook and notify fixtures from
/// <c>test/fixtures/hooks/codex/&lt;category&gt;/</c>, relative to the calling source file.
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
