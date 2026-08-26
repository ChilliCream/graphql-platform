namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Agent.Memory;

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
    public async Task JournalCollection_ExcludesCuratedMemories()
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
    public async Task JournalCollection_OrdersByCreatedAtDescending()
    {
        // arrange
        await InitWorkspaceAsync();
        var first = await SeedJournalEntryAsync("First entry.");
        FakeTime.Advance(TimeSpan.FromMinutes(1));
        var second = await SeedJournalEntryAsync("Second entry.");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "memory", "recent", "--collection", "journal");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var ids = document.RootElement.GetProperty("items")
            .EnumerateArray()
            .Select(e => e.GetProperty("id").GetString()!)
            .ToArray();

        Assert.Equal([second.Id, first.Id], ids);
    }

    [Fact]
    public async Task AllCollection_OrdersCuratedBandFirst_ThenJournalBand()
    {
        // arrange
        await InitWorkspaceAsync();
        var curated = await SeedMemoryAsync("Curated.");
        var journal = await SeedJournalEntryAsync("Journal.");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "memory", "recent", "--collection", "all");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var items = document.RootElement.GetProperty("items").EnumerateArray().ToArray();
        var collections = items.Select(item => item.GetProperty("collection").GetString()!).ToArray();
        var ids = items.Select(item => item.GetProperty("id").GetString()!).ToArray();

        Assert.Equal(["curated", "journal"], collections);
        Assert.Equal([curated.Id, journal.Id], ids);
    }

    [Fact]
    public async Task AllCollection_WithLimit_SplitsAcrossBands()
    {
        // arrange: three curated entries would fill an unsplit limit of 2
        // entirely, starving the journal band.
        await InitWorkspaceAsync();
        await SeedMemoryAsync("Curated one.");
        FakeTime.Advance(TimeSpan.FromMinutes(1));
        await SeedMemoryAsync("Curated two.");
        FakeTime.Advance(TimeSpan.FromMinutes(1));
        await SeedMemoryAsync("Curated three.");
        FakeTime.Advance(TimeSpan.FromMinutes(1));
        var journal = await SeedJournalEntryAsync("Journal.");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync(
            "agent", "memory", "recent", "--collection", "all", "--limit", "2");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var items = document.RootElement.GetProperty("items").EnumerateArray().ToArray();
        var collections = items.Select(item => item.GetProperty("collection").GetString()!).ToArray();
        var ids = items.Select(item => item.GetProperty("id").GetString()!).ToArray();

        Assert.Equal(["curated", "journal"], collections);
        Assert.Contains(journal.Id, ids);
    }

    [Fact]
    public async Task AllCollection_WithLimit_RemainderFlowsToShortBand()
    {
        // arrange: curated only has one entry, so its unused half of the
        // limit must flow to the journal band instead of going unused.
        await InitWorkspaceAsync();
        var curated = await SeedMemoryAsync("Curated.");
        FakeTime.Advance(TimeSpan.FromMinutes(1));
        await SeedJournalEntryAsync("Journal one.");
        FakeTime.Advance(TimeSpan.FromMinutes(1));
        await SeedJournalEntryAsync("Journal two.");
        FakeTime.Advance(TimeSpan.FromMinutes(1));
        await SeedJournalEntryAsync("Journal three.");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync(
            "agent", "memory", "recent", "--collection", "all", "--limit", "4");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var items = document.RootElement.GetProperty("items").EnumerateArray().ToArray();
        var collections = items.Select(item => item.GetProperty("collection").GetString()!).ToArray();
        var ids = items.Select(item => item.GetProperty("id").GetString()!).ToArray();

        Assert.Equal(["curated", "journal", "journal", "journal"], collections);
        Assert.Equal(curated.Id, ids[0]);
    }

    [Fact]
    public async Task AllCollection_WithLimit_EmptyJournalBand_FillsFromCurated()
    {
        // arrange: no journal entries at all, so the curated band's own
        // shortfall-remainder logic is not enough; the journal band coming
        // up empty must flow its whole share back to curated.
        await InitWorkspaceAsync();
        await SeedMemoryAsync("Curated one.");
        FakeTime.Advance(TimeSpan.FromMinutes(1));
        await SeedMemoryAsync("Curated two.");
        FakeTime.Advance(TimeSpan.FromMinutes(1));
        await SeedMemoryAsync("Curated three.");
        FakeTime.Advance(TimeSpan.FromMinutes(1));
        await SeedMemoryAsync("Curated four.");
        FakeTime.Advance(TimeSpan.FromMinutes(1));
        await SeedMemoryAsync("Curated five.");
        FakeTime.Advance(TimeSpan.FromMinutes(1));
        await SeedMemoryAsync("Curated six.");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync(
            "agent", "memory", "recent", "--collection", "all", "--limit", "4");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var items = document.RootElement.GetProperty("items").EnumerateArray().ToArray();
        var collections = items.Select(item => item.GetProperty("collection").GetString()!).ToArray();

        Assert.Equal(4, items.Length);
        Assert.All(collections, collection => Assert.Equal("curated", collection));
    }

    [Fact]
    public async Task NoWorkspace_ReportsTheMissingWorkspace()
    {
        // act
        var result = await ExecuteCommandAsync("agent", "memory", "recent");

        // assert
        result.AssertError("No agent workspace found. Run `nitro agent init` first.");
    }

    [Fact]
    public async Task DefaultScope_OrdersProjectBandFirst_ThenGlobalBand()
    {
        // arrange
        await InitWorkspaceAsync();
        var global = await SeedMemoryAsync("Global.");
        FakeTime.Advance(TimeSpan.FromMinutes(1));
        var project = await SeedMemoryAsync("Project.");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "memory", "recent");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var ids = document.RootElement.GetProperty("items")
            .EnumerateArray()
            .Select(e => e.GetProperty("id").GetString()!)
            .ToArray();

        Assert.Equal([project.Id, global.Id], ids);
    }
}
