using System.Runtime.CompilerServices;

namespace ChilliCream.Nitro.CommandLine.Tests.Hook;

/// <summary>
/// Reads Claude settings fixtures from
/// <c>test/fixtures/hooks/claude/&lt;category&gt;/</c>, relative to the calling source file.
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
