namespace HotChocolate.CostAnalysis;

/// <summary>
/// Discovers conformance fixture files copied next to the test assembly.
/// </summary>
internal static class FixtureLoader
{
    private const string ResourcesDirectoryName = "__resources__";
    private const string SchemaFileName = "fixture.schema.json";

    /// <summary>
    /// Enumerates every fixture file path, excluding the JSON schema itself.
    /// </summary>
    public static TheoryData<string> DiscoverFixturePaths()
    {
        var data = new TheoryData<string>();

        foreach (var path in Directory
            .EnumerateFiles(ResourcesDirectory, "*.json", SearchOption.AllDirectories)
            .Where(path => System.IO.Path.GetFileName(path) != SchemaFileName)
            // rust-corpus fixtures are not yet wired into the Fixture shape; hc-3-r5v.3 expands this filter.
            .Where(path => System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(path)) != "rust-corpus")
            .OrderBy(path => path, StringComparer.Ordinal))
        {
            data.Add(path);
        }

        return data;
    }

    /// <summary>
    /// Enumerates every fixture in one named family.
    /// </summary>
    public static TheoryData<string> Family(string name)
    {
        var data = new TheoryData<string>();
        var directory = System.IO.Path.Combine(ResourcesDirectory, name);

        if (!Directory.Exists(directory))
        {
            return data;
        }

        foreach (var path in Directory.EnumerateFiles(directory, "*.json").OrderBy(path => path, StringComparer.Ordinal))
        {
            data.Add(path);
        }

        return data;
    }

    public static int FixtureCount => DiscoverFixturePaths().Count;

    private static string ResourcesDirectory
        => System.IO.Path.Combine(AppContext.BaseDirectory, ResourcesDirectoryName);
}
