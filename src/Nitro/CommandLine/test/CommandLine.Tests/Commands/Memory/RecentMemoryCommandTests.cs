namespace ChilliCream.Nitro.CommandLine.Tests.Memory;

public sealed class RecentMemoryCommandTests(NitroCommandFixture fixture)
    : MemoryCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "memory", "recent", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              List the most recently updated memories, most recent first.

            Usage:
              nitro agent memory recent [options]

            Options:
              --collection <all|curated|journal>  The memory collection to show (curated, journal, or all) [default: curated]
              -n, --limit <limit>                 The maximum number of memories to show
              --output <json>                     The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                      Show help and usage information

            Example:
              nitro agent memory recent
              nitro agent memory recent --limit 5
              nitro agent memory recent --collection all
            """);
    }

    [Fact]
    public async Task NoMemories_PrintsEmptyMessage()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "memory", "recent");

        // assert
        result.AssertSuccess("No memories found.");
    }

    [Fact]
    public async Task MultipleMemories_OrdersByUpdatedAtDescending()
    {
        // arrange
        await InitWorkspaceAsync();
        var first = await SeedMemoryAsync("First.");
        FakeTime.Advance(TimeSpan.FromMinutes(1));
        var second = await SeedMemoryAsync("Second.");

        // act
        SetupInteractionMode(InteractionMode.JsonOutput);
        var result = await ExecuteCommandAsync("agent", "memory", "recent");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var ids = document.RootElement.GetProperty("items")
            .EnumerateArray()
            .Select(e => e.GetProperty("id").GetString()!)
            .ToArray();

        Assert.Equal([second.Id, first.Id], ids);
    }

    [Fact]
    public async Task WithLimit_LimitsResults()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedMemoryAsync("First.");
        FakeTime.Advance(TimeSpan.FromMinutes(1));
        var second = await SeedMemoryAsync("Second.");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "memory", "recent", "--limit", "1");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var ids = document.RootElement.GetProperty("items")
            .EnumerateArray()
            .Select(e => e.GetProperty("id").GetString()!)
            .ToArray();

        Assert.Equal([second.Id], ids);
    }

    [Fact]
    public async Task WithNAlias_LimitsResults()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedMemoryAsync("First.");
        FakeTime.Advance(TimeSpan.FromMinutes(1));
        var second = await SeedMemoryAsync("Second.");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "memory", "recent", "-n", "1");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var ids = document.RootElement.GetProperty("items")
            .EnumerateArray()
            .Select(e => e.GetProperty("id").GetString()!)
            .ToArray();

        Assert.Equal([second.Id], ids);
    }

    [Fact]
    public async Task JournalCollection_ReturnsEmpty_UntilJournalSliceLands()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedMemoryAsync("First.");

        // act
        var result = await ExecuteCommandAsync("agent", "memory", "recent", "--collection", "journal");

        // assert
        result.AssertSuccess("No memories found.");
    }

    [Fact]
    public async Task NoWorkspace_PrintsEmptyMessage()
    {
        // act
        var result = await ExecuteCommandAsync("agent", "memory", "recent");

        // assert
        result.AssertSuccess("No memories found.");
    }
}
