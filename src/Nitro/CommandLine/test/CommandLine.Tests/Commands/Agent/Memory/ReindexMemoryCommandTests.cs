namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Agent.Memory;

public sealed class ReindexMemoryCommandTests(NitroCommandFixture fixture)
    : MemoryCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "memory", "reindex", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Rebuild the memory search index. Repair only: normal reads already self-heal.

            Usage:
              nitro agent memory reindex [options]

            Options:
              --scope <all|global|project>  The memory scope to read from (project, global, or all) [default: all]
              --output <json>               The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                Show help and usage information

            Example:
              nitro agent memory reindex
              nitro agent memory reindex --scope global
            """);
    }

    [Fact]
    public async Task DefaultScope_RebuildsBothStores_WhenProjectWorkspaceExists()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedMemoryAsync("Project note.");
        await SeedMemoryAsync("Global note.", scope: "global");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "memory", "reindex");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var items = document.RootElement.GetProperty("items").EnumerateArray().ToArray();

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(2, items.Length);
        Assert.Equal("project", items[0].GetProperty("scope").GetString());
        Assert.Equal(1, items[0].GetProperty("indexedCount").GetInt32());
        Assert.Equal("global", items[1].GetProperty("scope").GetString());
        Assert.Equal(1, items[1].GetProperty("indexedCount").GetInt32());
    }

    [Fact]
    public async Task DefaultScope_SkipsProject_WhenNoWorkspaceExists()
    {
        // arrange
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "memory", "reindex");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var items = document.RootElement.GetProperty("items").EnumerateArray().ToArray();

        Assert.Equal(0, result.ExitCode);
        var row = Assert.Single(items);
        Assert.Equal("global", row.GetProperty("scope").GetString());
    }

    [Fact]
    public async Task ExplicitProjectScope_Throws_WhenNoWorkspaceExists()
    {
        // act
        var result = await ExecuteCommandAsync("agent", "memory", "reindex", "--scope", "project");

        // assert
        result.AssertError("No agent workspace found. Run `nitro agent init` first.");
    }

    [Fact]
    public async Task RebuildsIndex_AfterCorruption()
    {
        // arrange
        await InitWorkspaceAsync();
        var memory = await SeedMemoryAsync("Deploy checklist.");
        await ExecuteCommandAsync("agent", "memory", "search", "deploy");
        Assert.True(File.Exists(IndexPath));

        await File.WriteAllBytesAsync(
            IndexPath, "not a sqlite database"u8.ToArray(), TestContext.Current.CancellationToken);

        // act
        var reindexResult = await ExecuteCommandAsync("agent", "memory", "reindex", "--scope", "project");
        Assert.Equal(0, reindexResult.ExitCode);

        SetupInteractionMode(InteractionMode.JsonOutput);
        var searchResult = await ExecuteCommandAsync("agent", "memory", "search", "deploy");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(searchResult.StdOut);
        var ids = document.RootElement.GetProperty("items")
            .EnumerateArray()
            .Select(e => e.GetProperty("id").GetString()!)
            .ToArray();

        Assert.Equal([memory.Id], ids);
    }
}
