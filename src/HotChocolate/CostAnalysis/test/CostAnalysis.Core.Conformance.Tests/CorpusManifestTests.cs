using System.Text.Json;

namespace HotChocolate.CostAnalysis;

public sealed class CorpusManifestTests
{
    [Fact]
    public void Manifest_Should_MatchVendoredCorpus_When_Loaded()
    {
        // arrange
        var path = System.IO.Path.Combine(
            AppContext.BaseDirectory,
            "__resources__",
            "rust-corpus",
            "manifest.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var root = document.RootElement;

        // act
        var matrixSize = root.GetProperty("matrixSize").GetInt32();
        var exactCostRows = root.GetProperty("exactCostRows").GetInt32();
        var emitted = root.GetProperty("emitted").GetInt32();

        // assert
        Assert.Equal(15_840, matrixSize);
        Assert.Equal(1_980, exactCostRows);
        Assert.Equal(emitted, FixtureLoader.RustCorpusFixtureCount);
    }
}
