namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Agent.Memory;

public sealed class SearchMemoryCommandTests(NitroCommandFixture fixture)
    : MemoryCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "memory", "search", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Search curated memories by literal lexical match.

            Usage:
              nitro agent memory search <query> [options]

            Arguments:
              <query>  The search text, matched literally, never as FTS5 query syntax

            Options:
              --collection <all|curated|journal>  The memory collection to show (curated, journal, or all) [default: curated]
              --tag <tag>                         A tag; can be used multiple times
              --type <type>                       The memory type (fact, decision, preference, reference, or custom)
              --since <since>                     Only show memories updated at or after this RFC 3339 timestamp
              --scope <all|global|project>        The memory scope to read from (project, global, or all) [default: all]
              -n, --limit <limit>                 The maximum number of memories to show
              --output <json>                     The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                      Show help and usage information

            Example:
              nitro agent memory search "deploy checklist"
              nitro agent memory search deploy --tag ops --type decision
              nitro agent memory search deploy --since 2026-01-01T00:00:00Z
            """);
    }

    [Fact]
    public async Task NoMemories_PrintsEmptyMessage()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "memory", "search", "hello");

        // assert
        result.AssertSuccess("No memories found.");
    }

    [Fact]
    public async Task LiteralMatch_FindsMatchingMemory()
    {
        // arrange
        await InitWorkspaceAsync();
        var match = await SeedMemoryAsync("The deploy checklist covers staging first.");
        await SeedMemoryAsync("An unrelated entry about oceans.");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "memory", "search", "deploy");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var ids = document.RootElement.GetProperty("items")
            .EnumerateArray()
            .Select(e => e.GetProperty("id").GetString()!)
            .ToArray();

        Assert.Equal([match.Id], ids);
    }

    [Fact]
    public async Task QueryContainingFts5Operators_MatchesThemLiterally_NotAsSyntax()
    {
        // arrange
        await InitWorkspaceAsync();
        var literalMatch = await SeedMemoryAsync("Use quotes OR parentheses, never NOT unescaped.");
        await SeedMemoryAsync("A memory with none of those exact words.");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act: "OR" and "NOT" are FTS5 operators; search must match them as
        // literal words, not interpret them as query syntax.
        var result = await ExecuteCommandAsync("agent", "memory", "search", "quotes OR parentheses");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var ids = document.RootElement.GetProperty("items")
            .EnumerateArray()
            .Select(e => e.GetProperty("id").GetString()!)
            .ToArray();

        Assert.Equal([literalMatch.Id], ids);
    }

    [Fact]
    public async Task TagFilter_RequiresAllGivenTags()
    {
        // arrange
        await InitWorkspaceAsync();
        var both = await SeedMemoryAsync("Deploy notes.", tags: ["ops", "release"]);
        await SeedMemoryAsync("Deploy notes too.", tags: ["ops"]);
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync(
            "agent", "memory", "search", "deploy", "--tag", "ops", "--tag", "release");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var ids = document.RootElement.GetProperty("items")
            .EnumerateArray()
            .Select(e => e.GetProperty("id").GetString()!)
            .ToArray();

        Assert.Equal([both.Id], ids);
    }

    [Fact]
    public async Task TypeFilter_OnlyReturnsMatchingType()
    {
        // arrange
        await InitWorkspaceAsync();
        var decision = await SeedMemoryAsync("Deploy on Fridays is banned.", type: "decision");
        await SeedMemoryAsync("Deploy uses blue-green.", type: "fact");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "memory", "search", "deploy", "--type", "decision");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var ids = document.RootElement.GetProperty("items")
            .EnumerateArray()
            .Select(e => e.GetProperty("id").GetString()!)
            .ToArray();

        Assert.Equal([decision.Id], ids);
    }

    [Fact]
    public async Task SinceFilter_ExcludesOlderMemories()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedMemoryAsync("Deploy notes from the past.");
        FakeTime.Advance(TimeSpan.FromDays(1));
        var cutoff = FakeTime.GetUtcNow();
        FakeTime.Advance(TimeSpan.FromMinutes(1));
        var recent = await SeedMemoryAsync("Deploy notes from today.");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync(
            "agent", "memory", "search", "deploy", "--since", cutoff.ToString("O"));

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var ids = document.RootElement.GetProperty("items")
            .EnumerateArray()
            .Select(e => e.GetProperty("id").GetString()!)
            .ToArray();

        Assert.Equal([recent.Id], ids);
    }

    [Fact]
    public async Task JournalCollection_ExcludesCuratedMemories()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedMemoryAsync("Deploy notes.");

        // act
        var result = await ExecuteCommandAsync(
            "agent", "memory", "search", "deploy", "--collection", "journal");

        // assert
        result.AssertSuccess("No memories found.");
    }

    [Fact]
    public async Task JournalCollection_FindsMatchingJournalEntry()
    {
        // arrange
        await InitWorkspaceAsync();
        var match = await SeedJournalEntryAsync("Deploy checklist covers staging first.");
        await SeedJournalEntryAsync("An unrelated entry about oceans.");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync(
            "agent", "memory", "search", "deploy", "--collection", "journal");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var items = document.RootElement.GetProperty("items").EnumerateArray().ToArray();
        var item = Assert.Single(items);

        Assert.Equal(match.Id, item.GetProperty("id").GetString());
        Assert.Equal("journal", item.GetProperty("collection").GetString());
    }

    [Fact]
    public async Task JournalCollection_WithTagFilter_ReturnsEmpty()
    {
        // arrange: a journal entry has no tags until it is promoted, so a
        // `--tag` filter can never match it and must exclude the journal
        // band entirely rather than fall back to unfiltered entries.
        await InitWorkspaceAsync();
        await SeedJournalEntryAsync("Deploy checklist covers staging first.");

        // act
        var result = await ExecuteCommandAsync(
            "agent", "memory", "search", "deploy", "--collection", "journal", "--tag", "ops");

        // assert
        result.AssertSuccess("No memories found.");
    }

    [Fact]
    public async Task JournalCollection_WithTypeFilter_ReturnsEmpty()
    {
        // arrange: a journal entry has no type until it is promoted, so a
        // `--type` filter can never match it and must exclude the journal
        // band entirely rather than fall back to unfiltered entries.
        await InitWorkspaceAsync();
        await SeedJournalEntryAsync("Deploy checklist covers staging first.");

        // act
        var result = await ExecuteCommandAsync(
            "agent", "memory", "search", "deploy", "--collection", "journal", "--type", "decision");

        // assert
        result.AssertSuccess("No memories found.");
    }

    [Fact]
    public async Task AllCollection_WithTagFilter_ExcludesJournalBand()
    {
        // arrange: `--collection all --tag` validly narrows the curated
        // band while the journal band, which can never satisfy a tag
        // filter, is excluded entirely rather than returned unfiltered.
        await InitWorkspaceAsync();
        var curated = await SeedMemoryAsync("Deploy notes from curated.", tags: ["ops"]);
        await SeedJournalEntryAsync("Deploy notes from journal.");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync(
            "agent", "memory", "search", "deploy", "--collection", "all", "--tag", "ops");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var items = document.RootElement.GetProperty("items").EnumerateArray().ToArray();
        var item = Assert.Single(items);

        Assert.Equal(curated.Id, item.GetProperty("id").GetString());
        Assert.Equal("curated", item.GetProperty("collection").GetString());
    }

    [Fact]
    public async Task AllCollection_IncludesBothCuratedAndJournalMatches()
    {
        // arrange
        await InitWorkspaceAsync();
        var curated = await SeedMemoryAsync("Deploy notes from curated.");
        var journal = await SeedJournalEntryAsync("Deploy notes from journal.");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync(
            "agent", "memory", "search", "deploy", "--collection", "all");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var items = document.RootElement.GetProperty("items").EnumerateArray().ToArray();
        var collections = items.Select(item => item.GetProperty("collection").GetString()!).ToArray();
        var ids = items.Select(item => item.GetProperty("id").GetString()!).ToArray();

        // Collection band, curated first.
        Assert.Equal(["curated", "journal"], collections);
        Assert.Equal([curated.Id, journal.Id], ids);
    }

    [Fact]
    public async Task AllCollection_WithLimit_SplitsAcrossBands()
    {
        // arrange: three curated matches would fill an unsplit limit of 2
        // entirely, starving the journal band even though it also matches.
        await InitWorkspaceAsync();
        await SeedMemoryAsync("Deploy notes one.");
        FakeTime.Advance(TimeSpan.FromMinutes(1));
        await SeedMemoryAsync("Deploy notes two.");
        FakeTime.Advance(TimeSpan.FromMinutes(1));
        await SeedMemoryAsync("Deploy notes three.");
        FakeTime.Advance(TimeSpan.FromMinutes(1));
        var journal = await SeedJournalEntryAsync("Deploy notes from journal.");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync(
            "agent", "memory", "search", "deploy", "--collection", "all", "--limit", "2");

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
        // arrange: curated only has one match, so its unused half of the
        // limit must flow to the journal band instead of going unused.
        await InitWorkspaceAsync();
        var curated = await SeedMemoryAsync("Deploy notes from curated.");
        FakeTime.Advance(TimeSpan.FromMinutes(1));
        await SeedJournalEntryAsync("Deploy notes from journal one.");
        FakeTime.Advance(TimeSpan.FromMinutes(1));
        await SeedJournalEntryAsync("Deploy notes from journal two.");
        FakeTime.Advance(TimeSpan.FromMinutes(1));
        await SeedJournalEntryAsync("Deploy notes from journal three.");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync(
            "agent", "memory", "search", "deploy", "--collection", "all", "--limit", "4");

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
        // arrange: no journal matches at all, so the curated band's own
        // shortfall-remainder logic is not enough; the journal band coming
        // up empty must flow its whole share back to curated.
        await InitWorkspaceAsync();
        await SeedMemoryAsync("Deploy notes one.");
        FakeTime.Advance(TimeSpan.FromMinutes(1));
        await SeedMemoryAsync("Deploy notes two.");
        FakeTime.Advance(TimeSpan.FromMinutes(1));
        await SeedMemoryAsync("Deploy notes three.");
        FakeTime.Advance(TimeSpan.FromMinutes(1));
        await SeedMemoryAsync("Deploy notes four.");
        FakeTime.Advance(TimeSpan.FromMinutes(1));
        await SeedMemoryAsync("Deploy notes five.");
        FakeTime.Advance(TimeSpan.FromMinutes(1));
        await SeedMemoryAsync("Deploy notes six.");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync(
            "agent", "memory", "search", "deploy", "--collection", "all", "--limit", "4");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var items = document.RootElement.GetProperty("items").EnumerateArray().ToArray();
        var collections = items.Select(item => item.GetProperty("collection").GetString()!).ToArray();

        Assert.Equal(4, items.Length);
        Assert.All(collections, collection => Assert.Equal("curated", collection));
    }

    [Fact]
    public async Task DefaultScope_OrdersProjectBandFirst_ThenGlobalBand()
    {
        // arrange
        await InitWorkspaceAsync();
        var global = await SeedMemoryAsync("Deploy from global.", scope: "global");
        FakeTime.Advance(TimeSpan.FromMinutes(1));
        var project = await SeedMemoryAsync("Deploy from project.", scope: "project");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "memory", "search", "deploy");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var ids = document.RootElement.GetProperty("items")
            .EnumerateArray()
            .Select(e => e.GetProperty("id").GetString()!)
            .ToArray();

        Assert.Equal([project.Id, global.Id], ids);
    }

    [Fact]
    public async Task ScopeFilter_Project_OnlyReturnsProjectMemories()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedMemoryAsync("Deploy from global.", scope: "global");
        var project = await SeedMemoryAsync("Deploy from project.", scope: "project");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "memory", "search", "deploy", "--scope", "project");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var ids = document.RootElement.GetProperty("items")
            .EnumerateArray()
            .Select(e => e.GetProperty("id").GetString()!)
            .ToArray();

        Assert.Equal([project.Id], ids);
    }

    [Fact]
    public async Task WithLimit_LimitsResultsAcrossBands()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedMemoryAsync("Deploy from global.", scope: "global");
        FakeTime.Advance(TimeSpan.FromMinutes(1));
        var project = await SeedMemoryAsync("Deploy from project.", scope: "project");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "memory", "search", "deploy", "--limit", "1");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var ids = document.RootElement.GetProperty("items")
            .EnumerateArray()
            .Select(e => e.GetProperty("id").GetString()!)
            .ToArray();

        Assert.Equal([project.Id], ids);
    }

    // The three self-healing scenarios the ticket's index coherence contract
    // requires: a fresh clone (markdown present, no index yet), a manual
    // edit outside the CLI (stale fingerprint), and an interrupted write
    // (corrupt index file). In every case, `search` must transparently
    // rebuild rather than crash or serve wrong results.

    [Fact]
    public async Task SelfHealing_FreshClone_BuildsIndexOnFirstSearch()
    {
        // arrange
        await InitWorkspaceAsync();
        var match = await SeedMemoryAsync("Deploy checklist for staging.");
        Assert.False(File.Exists(IndexPath));
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "memory", "search", "deploy");

        // assert
        Assert.True(File.Exists(IndexPath));
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var ids = document.RootElement.GetProperty("items")
            .EnumerateArray()
            .Select(e => e.GetProperty("id").GetString()!)
            .ToArray();

        Assert.Equal([match.Id], ids);
    }

    [Fact]
    public async Task SelfHealing_ManualEdit_RebuildsStaleIndex()
    {
        // arrange
        await InitWorkspaceAsync();
        var memory = await SeedMemoryAsync("Deploy checklist for staging.");
        SetupInteractionMode(InteractionMode.JsonOutput);
        await ExecuteCommandAsync("agent", "memory", "search", "deploy");
        Assert.True(File.Exists(IndexPath));

        // A manual edit outside the CLI: the file's mtime changes, which
        // must invalidate the index's stored fingerprint.
        var content = await File.ReadAllTextAsync(memory.Path, TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(
            memory.Path,
            content.Replace("Deploy checklist for staging.", "Rollback checklist for staging."),
            TestContext.Current.CancellationToken);

        // act
        var result = await ExecuteCommandAsync("agent", "memory", "search", "rollback");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var ids = document.RootElement.GetProperty("items")
            .EnumerateArray()
            .Select(e => e.GetProperty("id").GetString()!)
            .ToArray();

        Assert.Equal([memory.Id], ids);
    }

    [Fact]
    public async Task SelfHealing_InterruptedWrite_RebuildsCorruptIndex()
    {
        // arrange
        await InitWorkspaceAsync();
        var memory = await SeedMemoryAsync("Deploy checklist for staging.");
        SetupInteractionMode(InteractionMode.JsonOutput);
        await ExecuteCommandAsync("agent", "memory", "search", "deploy");
        Assert.True(File.Exists(IndexPath));

        // Simulate a process killed mid-rebuild: the index file is present
        // but truncated garbage, not a valid SQLite database.
        await File.WriteAllBytesAsync(
            IndexPath, "not a sqlite database"u8.ToArray(), TestContext.Current.CancellationToken);

        // act
        var result = await ExecuteCommandAsync("agent", "memory", "search", "deploy");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var ids = document.RootElement.GetProperty("items")
            .EnumerateArray()
            .Select(e => e.GetProperty("id").GetString()!)
            .ToArray();

        Assert.Equal([memory.Id], ids);
    }

    [Fact]
    public async Task MalformedFrontmatter_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedMemoryAsync("Deploy checklist.");
        await File.WriteAllTextAsync(
            Path.Combine(CuratedDirectory, "broken-id.md"),
            "not frontmatter at all",
            TestContext.Current.CancellationToken);

        // act
        var result = await ExecuteCommandAsync("agent", "memory", "search", "deploy");

        // assert
        result.AssertError(
            "Memory 'broken-id' has malformed frontmatter: "
            + "Frontmatter must start with a '---' delimiter line.");
    }

    [Fact]
    public async Task CrossScopeDuplicateId_ReturnsErrorWithNoPartialOutput()
    {
        // arrange
        await InitWorkspaceAsync();
        var projectRecord = await SeedMemoryAsync("Deploy checklist.", scope: "project");
        Directory.CreateDirectory(GlobalCuratedDirectory);
        File.Copy(projectRecord.Path, Path.Combine(GlobalCuratedDirectory, projectRecord.Id + ".md"));

        // act
        var result = await ExecuteCommandAsync("agent", "memory", "search", "deploy");

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Empty(result.StdOut);
        Assert.Contains("Cross-scope duplicate memory ids found", result.StdErr);
    }
}
