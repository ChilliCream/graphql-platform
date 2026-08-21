namespace ChilliCream.Nitro.CommandLine.Tests.Memory;

public sealed class TagsMemoryCommandTests(NitroCommandFixture fixture)
    : MemoryCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "memory", "tags", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              List curated memory tags in use, with counts per scope.

            Usage:
              nitro agent memory tags [options]

            Options:
              --scope <all|global|project>  The memory scope to read from (project, global, or all) [default: all]
              --output <json>               The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                Show help and usage information

            Example:
              nitro agent memory tags
              nitro agent memory tags --scope global
            """);
    }

    [Fact]
    public async Task NoTags_PrintsEmptyMessage()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "memory", "tags");

        // assert
        result.AssertSuccess("No tags.");
    }

    [Fact]
    public async Task CountsAreLexicallyOrdered_AndSplitByScope()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedMemoryAsync("Project one.", tags: ["ops"]);
        await SeedMemoryAsync("Project two.", tags: ["ops", "release"]);
        await SeedMemoryAsync("Global one.", tags: ["ops"], scope: "global");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "memory", "tags");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var items = document.RootElement.GetProperty("items").EnumerateArray().ToArray();

        Assert.Equal(2, items.Length);

        Assert.Equal("ops", items[0].GetProperty("tag").GetString());
        Assert.Equal(2, items[0].GetProperty("projectCount").GetInt32());
        Assert.Equal(1, items[0].GetProperty("globalCount").GetInt32());
        Assert.Equal(3, items[0].GetProperty("totalCount").GetInt32());

        Assert.Equal("release", items[1].GetProperty("tag").GetString());
        Assert.Equal(1, items[1].GetProperty("projectCount").GetInt32());
        Assert.Equal(0, items[1].GetProperty("globalCount").GetInt32());
        Assert.Equal(1, items[1].GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task ScopeFilter_Project_OnlyCountsProjectMemories()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedMemoryAsync("Project one.", tags: ["ops"]);
        await SeedMemoryAsync("Global one.", tags: ["ops"], scope: "global");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "memory", "tags", "--scope", "project");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var items = document.RootElement.GetProperty("items").EnumerateArray().ToArray();
        var row = Assert.Single(items);

        Assert.Equal("ops", row.GetProperty("tag").GetString());
        Assert.Equal(1, row.GetProperty("projectCount").GetInt32());
        Assert.Equal(0, row.GetProperty("globalCount").GetInt32());
        Assert.Equal(1, row.GetProperty("totalCount").GetInt32());
    }
}
