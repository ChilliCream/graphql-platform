using System.Runtime.CompilerServices;

namespace ChilliCream.Nitro.CommandLine.Tests.Hook;

/// <summary>
/// Reads captured Claude hook payload fixtures from
/// <c>test/fixtures/hooks/claude/</c>, relative to the calling source file.
/// </summary>
internal static class HookFixtures
{
    public static string Read(string fileName, [CallerFilePath] string sourceFile = "")
    {
        var directory = Path.GetDirectoryName(sourceFile)!;
        var path = Path.Combine(directory, "..", "..", "..", "fixtures", "hooks", "claude", fileName);

        return File.ReadAllText(path);
    }
}
