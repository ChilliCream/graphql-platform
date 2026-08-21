namespace ChilliCream.Nitro.CommandLine.Tests.Memory;

public sealed class PromoteMemoryCommandTests(NitroCommandFixture fixture)
    : MemoryCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "memory", "promote", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Promote a journal entry into a curated memory. Mechanical copy only: no summarization or heuristics.

            Usage:
              nitro agent memory promote [<journal-id>] [options]

            Arguments:
              <journal-id>  The journal entry ID. Omit to list unpromoted candidates

            Options:
              --type <type>                 The memory type (fact, decision, preference, reference, or custom)
              --tag <tag>                   A tag; can be used multiple times
              --scope <all|global|project>  The memory scope to read from (project, global, or all) [default: all]
              --output <json>               The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                Show help and usage information

            Example:
              nitro agent memory promote
              nitro agent memory promote "01hqzxk8xdtd3fk3f0z7c5g8vm" --type decision --tag ops
            """);
    }

    [Fact]
    public async Task WithId_PromotesJournalEntry_IntoCuratedMemory()
    {
        // arrange
        await InitWorkspaceAsync();
        var entry = await SeedJournalEntryAsync("Investigated the flaky test.");

        // act
        var result = await ExecuteCommandAsync(
            "agent", "memory", "promote", entry.Id, "--type", "decision", "--tag", "flaky");

        // assert
        Assert.Equal(0, result.ExitCode);
        var curatedFile = Directory.GetFiles(CuratedDirectory, "*.md").Single();
        var content = await File.ReadAllTextAsync(curatedFile, TestContext.Current.CancellationToken);
        Assert.Contains("type: decision", content);
        Assert.Contains("tags: [flaky]", content);
        Assert.Contains($"promoted_from: {entry.Id}", content);
        Assert.Contains("Investigated the flaky test.", content);
    }

    [Fact]
    public async Task JsonOutput_ReturnsPromotionResult_WithAlreadyPromotedFalse()
    {
        // arrange
        await InitWorkspaceAsync();
        var entry = await SeedJournalEntryAsync("Investigated the flaky test.");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync(
            "agent", "memory", "promote", entry.Id, "--type", "decision");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var root = document.RootElement;

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("decision", root.GetProperty("type").GetString());
        Assert.Equal(entry.Id, root.GetProperty("promotedFrom").GetString());
        Assert.False(root.GetProperty("alreadyPromoted").GetBoolean());
    }

    [Fact]
    public async Task PromoteTwice_IsIdempotent_ReturnsSameRecordWithAlreadyPromotedTrue()
    {
        // arrange
        await InitWorkspaceAsync();
        var entry = await SeedJournalEntryAsync("Investigated the flaky test.");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var first = await ExecuteCommandAsync(
            "agent", "memory", "promote", entry.Id, "--type", "decision");
        var second = await ExecuteCommandAsync(
            "agent", "memory", "promote", entry.Id, "--type", "decision");

        // assert
        using var firstDocument = System.Text.Json.JsonDocument.Parse(first.StdOut);
        using var secondDocument = System.Text.Json.JsonDocument.Parse(second.StdOut);

        Assert.False(firstDocument.RootElement.GetProperty("alreadyPromoted").GetBoolean());
        Assert.True(secondDocument.RootElement.GetProperty("alreadyPromoted").GetBoolean());
        Assert.Equal(
            firstDocument.RootElement.GetProperty("id").GetString(),
            secondDocument.RootElement.GetProperty("id").GetString());
        Assert.Single(Directory.GetFiles(CuratedDirectory, "*.md"));
    }

    [Fact]
    public async Task ConcurrentPromotes_SameJournalEntry_BothSucceed_OnlyOneCuratedFile()
    {
        // arrange
        await InitWorkspaceAsync();
        var entry = await SeedJournalEntryAsync("Investigated the flaky test.");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act: two "simultaneous" promotions of the same journal entry.
        var firstTask = ExecuteCommandAsync("agent", "memory", "promote", entry.Id, "--type", "decision");
        var secondTask = ExecuteCommandAsync("agent", "memory", "promote", entry.Id, "--type", "decision");
        var results = await Task.WhenAll(firstTask, secondTask);

        // assert
        Assert.All(results, result => Assert.Equal(0, result.ExitCode));
        var ids = results
            .Select(result => System.Text.Json.JsonDocument.Parse(result.StdOut).RootElement
                .GetProperty("id").GetString())
            .Distinct()
            .ToArray();
        Assert.Single(ids);
        Assert.Single(Directory.GetFiles(CuratedDirectory, "*.md"));
    }

    [Fact]
    public async Task MissingType_ReturnsParseError()
    {
        // arrange
        await InitWorkspaceAsync();
        var entry = await SeedJournalEntryAsync("Investigated the flaky test.");

        // act
        var result = await ExecuteCommandAsync("agent", "memory", "promote", entry.Id);

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Contains("Option '--type' is required when promoting a journal entry.", result.StdErr);
    }

    [Fact]
    public async Task TypeWithoutId_ReturnsParseError()
    {
        // act
        var result = await ExecuteCommandAsync("agent", "memory", "promote", "--type", "decision");

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Contains("'--type' and '--tag' require a journal entry id.", result.StdErr);
    }

    [Fact]
    public async Task JournalEntryDoesNotExist_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync(
            "agent", "memory", "promote", "01hqzxk8xdtd3fk3f0z7c5g8vm", "--type", "decision");

        // assert
        result.AssertError("Journal entry '01hqzxk8xdtd3fk3f0z7c5g8vm' does not exist.");
    }

    [Fact]
    public async Task Bare_ListsUnpromotedCandidates_ExcludingAlreadyPromoted()
    {
        // arrange
        await InitWorkspaceAsync();
        var promoted = await SeedJournalEntryAsync("Already promoted.");
        var unpromoted = await SeedJournalEntryAsync("Not promoted yet.");
        await ExecuteCommandAsync("agent", "memory", "promote", promoted.Id, "--type", "fact");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "memory", "promote");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var ids = document.RootElement.GetProperty("items")
            .EnumerateArray()
            .Select(e => e.GetProperty("id").GetString()!)
            .ToArray();

        Assert.Equal(0, result.ExitCode);
        Assert.Equal([unpromoted.Id], ids);
    }

    [Fact]
    public async Task Bare_NoUnpromotedCandidates_PrintsEmptyMessage()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "memory", "promote");

        // assert
        result.AssertSuccess("No unpromoted journal entries found.");
    }
}
