namespace HotChocolate.CostAnalysis;

/// <summary>
/// Discovers the linked Conformance.Tests precision fixture files copied
/// next to this test assembly.
/// </summary>
public static class PrecisionFixtureLoader
{
    private const string ResourcesDirectoryName = "__resources__";
    private const string PrecisionDirectoryName = "precision";

    public static TheoryData<string> FixturePaths()
    {
        var data = new TheoryData<string>();
        var directory = System.IO.Path.Combine(
            AppContext.BaseDirectory,
            ResourcesDirectoryName,
            PrecisionDirectoryName);

        foreach (var path in Directory
            .EnumerateFiles(directory, "*.json")
            .OrderBy(path => path, StringComparer.Ordinal))
        {
            data.Add(path);
        }

        return data;
    }
}
