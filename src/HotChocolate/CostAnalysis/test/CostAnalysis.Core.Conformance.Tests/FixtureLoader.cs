using System.Text.Json;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// Discovers conformance fixture files copied next to the test assembly.
/// </summary>
internal static class FixtureLoader
{
    private const string ResourcesDirectoryName = "__resources__";
    private const string SchemaFileName = "fixture.schema.json";
    private const string RustCorpusDirectoryName = "rust-corpus";
    private const string RustCorpusFileName = "exact-case.json";
    private const string RustCorpusDisplayPrefix = "rust-corpus/";
    private static readonly Lazy<IReadOnlyDictionary<string, Fixture>> s_rustCorpus =
        new(LoadRustCorpus);

    /// <summary>
    /// Enumerates every fixture file path, excluding the JSON schema itself.
    /// </summary>
    public static TheoryData<string> DiscoverFixturePaths()
    {
        var data = new TheoryData<string>();

        foreach (var path in Directory
            .EnumerateFiles(ResourcesDirectory, "*.json", SearchOption.AllDirectories)
            .Where(path => System.IO.Path.GetFileName(path) != SchemaFileName)
            .Where(path => System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(path)) != RustCorpusDirectoryName)
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

    /// <summary>
    /// Enumerates the display names of every fixture in the vendored Rust corpus.
    /// </summary>
    public static TheoryData<string> RustCorpus()
    {
        var data = new TheoryData<string>();

        foreach (var displayName in s_rustCorpus.Value.Keys.OrderBy(name => name, StringComparer.Ordinal))
        {
            data.Add(new TheoryDataRow<string>(displayName).WithTestDisplayName(displayName));
        }

        return data;
    }

    /// <summary>
    /// Gets the Rust corpus fixture identified by its theory display name.
    /// </summary>
    public static Fixture LoadRustCorpusFixture(string displayName)
        => s_rustCorpus.Value[displayName];

    public static int FixtureCount => DiscoverFixturePaths().Count;

    public static int RustCorpusFixtureCount => s_rustCorpus.Value.Count;

    private static string ResourcesDirectory
        => System.IO.Path.Combine(AppContext.BaseDirectory, ResourcesDirectoryName);

    private static IReadOnlyDictionary<string, Fixture> LoadRustCorpus()
    {
        var path = System.IO.Path.Combine(
            ResourcesDirectory,
            RustCorpusDirectoryName,
            RustCorpusFileName);
        var fixtures = JsonSerializer.Deserialize<Fixture[]>(File.ReadAllText(path)) ?? [];

        return fixtures.ToDictionary(
            fixture => RustCorpusDisplayPrefix + fixture.Id,
            StringComparer.Ordinal);
    }
}
