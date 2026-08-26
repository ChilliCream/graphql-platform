using ChilliCream.Nitro.CommandLine.Services.Memory;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Agent.Memory;

public sealed class ContextMemoryCommandTests(NitroCommandFixture fixture)
    : MemoryCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "memory", "context", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Assemble curated memories into a prompt-ready block within a character budget.

            Usage:
              nitro agent memory context [options]

            Options:
              --tag <tag>              A tag; can be used multiple times
              --limit <limit>          The maximum number of memories to admit [default: 50]
              --max-chars <max-chars>  The character budget for the assembled prompt-ready text [default: 20000]
              --output <json>          The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help           Show help and usage information

            Example:
              nitro agent memory context
              nitro agent memory context --tag onboarding
              nitro agent memory context --max-chars 4000
            """);
    }

    [Fact]
    public async Task NoMemories_PrintsEmptyMessage()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "memory", "context");

        // assert
        result.AssertSuccess("No memories found.");
    }

    [Fact]
    public async Task AdmitsWholeEntries_ProjectBandFirst_ThenGlobalBand()
    {
        // arrange
        await InitWorkspaceAsync();
        var global = await SeedMemoryAsync("Global note.", scope: "global");
        FakeTime.Advance(TimeSpan.FromMinutes(1));
        var project = await SeedMemoryAsync("Project note.", scope: "project");

        // act
        var result = await ExecuteCommandAsync("agent", "memory", "context");

        // assert
        Assert.Equal(0, result.ExitCode);
        var projectIndex = result.StdOut.IndexOf(project.Id, StringComparison.Ordinal);
        var globalIndex = result.StdOut.IndexOf(global.Id, StringComparison.Ordinal);
        Assert.True(projectIndex >= 0 && globalIndex >= 0 && projectIndex < globalIndex);
    }

    [Fact]
    public async Task NeverIncludesJournal()
    {
        // arrange: journal capture does not exist yet in this slice, so this
        // just pins that context stays curated-only once it does.
        await InitWorkspaceAsync();
        await SeedMemoryAsync("Curated note.");

        // act
        var result = await ExecuteCommandAsync("agent", "memory", "context");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.DoesNotContain("journal", result.StdOut, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TagFilter_RequiresAllGivenTags()
    {
        // arrange
        await InitWorkspaceAsync();
        var both = await SeedMemoryAsync("Both tags.", tags: ["ops", "release"]);
        await SeedMemoryAsync("One tag.", tags: ["ops"]);
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync(
            "agent", "memory", "context", "--tag", "ops", "--tag", "release");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var ids = document.RootElement.GetProperty("entries")
            .EnumerateArray()
            .Select(e => e.GetProperty("id").GetString()!)
            .ToArray();

        Assert.Equal([both.Id], ids);
    }

    [Fact]
    public async Task LimitOption_CapsAdmittedEntryCount()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedMemoryAsync("First.");
        FakeTime.Advance(TimeSpan.FromMinutes(1));
        var second = await SeedMemoryAsync("Second.");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "memory", "context", "--limit", "1");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var ids = document.RootElement.GetProperty("entries")
            .EnumerateArray()
            .Select(e => e.GetProperty("id").GetString()!)
            .ToArray();

        Assert.Equal([second.Id], ids);
    }

    [Fact]
    public async Task MaxCharsOption_StopsAdmittingBeforeExceedingBudget()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedMemoryAsync("Older note.");
        FakeTime.Advance(TimeSpan.FromMinutes(1));
        var newest = await SeedMemoryAsync("Newer note.");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // A budget that fits the newest (rank-first) entry exactly, with no
        // room for a second.
        var maxChars = MemoryContextRenderer.RenderEntry(newest).Length;

        // act: the budget admits exactly the first-ranked entry and stops
        // rather than truncating it or skipping ahead to a smaller one.
        var result = await ExecuteCommandAsync(
            "agent", "memory", "context", "--max-chars", maxChars.ToString());

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var ids = document.RootElement.GetProperty("entries")
            .EnumerateArray()
            .Select(e => e.GetProperty("id").GetString()!)
            .ToArray();

        Assert.Equal([newest.Id], ids);
    }

    [Fact]
    public async Task FirstEntryAloneExceedsBudget_ReturnsNoEntries_AndReportsOmission()
    {
        // arrange
        await InitWorkspaceAsync();
        var oversized = await SeedMemoryAsync(new string('a', 100));
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "memory", "context", "--max-chars", "10");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        Assert.Empty(document.RootElement.GetProperty("entries").EnumerateArray());
        Assert.Equal(oversized.Id, document.RootElement.GetProperty("omittedEntryId").GetString());
        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public async Task CrossScopeDuplicateId_ReturnsErrorWithNoPartialOutput()
    {
        // arrange
        await InitWorkspaceAsync();
        var projectRecord = await SeedMemoryAsync("Note.", scope: "project");
        Directory.CreateDirectory(GlobalCuratedDirectory);
        File.Copy(projectRecord.Path, Path.Combine(GlobalCuratedDirectory, projectRecord.Id + ".md"));

        // act
        var result = await ExecuteCommandAsync("agent", "memory", "context");

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Empty(result.StdOut);
        Assert.Contains("Cross-scope duplicate memory ids found", result.StdErr);
    }
}
