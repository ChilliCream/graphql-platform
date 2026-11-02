namespace ChilliCream.Nitro.CommandLine.Tests.Hook;

internal static class OpencodeHookFixtures
{
    public static string Read(string fileName, [System.Runtime.CompilerServices.CallerFilePath] string sourceFile = "")
    {
        var directory = Path.GetDirectoryName(sourceFile)!;
        var path = Path.Combine(directory, "..", "..", "..", "fixtures", "hooks", "opencode", fileName);

        return File.ReadAllText(path);
    }
}
