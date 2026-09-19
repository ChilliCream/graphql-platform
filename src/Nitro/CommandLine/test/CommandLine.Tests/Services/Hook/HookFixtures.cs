using System.Runtime.CompilerServices;

namespace ChilliCream.Nitro.CommandLine.Tests.Hook;

/// <summary>
/// Reads captured Claude hook payload fixtures from <c>test/fixtures/hooks/claude/</c>,
/// resolved relative to this file's own source path so it works regardless of the test run's
/// working directory.
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
