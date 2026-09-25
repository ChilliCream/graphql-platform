using System.Runtime.CompilerServices;

namespace ChilliCream.Nitro.CommandLine.Tests;

public sealed class DapperCommandDefinitionUsageTests
{
    [Fact]
    public void SourceFiles_Should_NotContainCommandDefinition_When_UnderServicesOrCommands()
    {
        // arrange
        // Dapper.AOT does not intercept CommandDefinition calls, which crash under native AOT.
        var sourceRoot = GetCommandLineSourceRoot();
        var scannedDirectories = new[] { "Services", "Commands" };

        // act
        var offenders = new List<string>();

        foreach (var directory in scannedDirectories)
        {
            var root = Path.Combine(sourceRoot, directory);

            foreach (var file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                if (File.ReadAllText(file).Contains("new CommandDefinition(", StringComparison.Ordinal))
                {
                    offenders.Add(Path.GetRelativePath(sourceRoot, file));
                }
            }
        }

        // assert
        Assert.Equal([], offenders);
    }

    private static string GetCommandLineSourceRoot([CallerFilePath] string sourceFile = "")
    {
        var directory = Path.GetDirectoryName(sourceFile)!;

        return Path.GetFullPath(Path.Combine(directory, "..", "..", "src", "CommandLine"));
    }
}
