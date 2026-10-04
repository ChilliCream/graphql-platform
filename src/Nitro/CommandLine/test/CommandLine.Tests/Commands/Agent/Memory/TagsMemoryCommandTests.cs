namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Agent.Memory;

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
              --output <json>  The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help   Show help and usage information

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
    public async Task CountsAreLexicallyOrdered()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedMemoryAsync("Project one.", tags: ["ops"]);
        await SeedMemoryAsync("Project two.", tags: ["ops", "release"]);
        await SeedMemoryAsync("Global one.", tags: ["ops"]);
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "memory", "tags");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var items = document.RootElement.GetProperty("items").EnumerateArray().ToArray();

        Assert.Equal(2, items.Length);

        Assert.Equal("ops", items[0].GetProperty("tag").GetString());

        Assert.Equal("release", items[1].GetProperty("tag").GetString());
    }
}
