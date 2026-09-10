namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Agent.Memory;

public sealed class LogMemoryCommandTests(NitroCommandFixture fixture)
    : MemoryCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "memory", "log", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Capture a cheap journal entry. No type or tags at capture time; assign those when promoting.

            Usage:
              nitro agent memory log [<text>] [options]

            Arguments:
              <text>  The memory text. Exactly one of the text argument or --file is required

            Options:
              --file <file>               A file to read the memory text from
              --actor <actor> (REQUIRED)  The actor performing this command; allocate one with `nitro agent login`
              --output <json>             The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help              Show help and usage information

            Example:
              nitro agent memory log "Investigated the flaky test; still unresolved."
              nitro agent memory log --file session-notes.md
            """);
    }

    [Fact]
    public async Task WithText_LogsSuccessfully()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync(
            "agent", "memory", "log", "Investigated the flaky test; still unresolved.");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Single(await ReadJournalAsync());
    }

    [Fact]
    public async Task WithText_WritesUnderUtcDateDirectory()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "memory", "log", "Some captured text.");

        // assert
        Assert.Equal(0, result.ExitCode);
        var logged = Assert.Single(await ReadJournalAsync());
        Assert.Equal(FakeTime.GetUtcNow(), logged.CreatedAt);
    }

    [Fact]
    public async Task WithFile_ReadsText()
    {
        // arrange
        await InitWorkspaceAsync();
        var filePath = Path.Combine(WorkingDirectory, "note.md");
        await File.WriteAllTextAsync(filePath, "Line one\nLine two\n", TestContext.Current.CancellationToken);

        // act
        var result = await ExecuteCommandAsync("agent", "memory", "log", "--file", "note.md");

        // assert
        Assert.Equal(0, result.ExitCode);
        var logged = Assert.Single(await ReadJournalAsync());
        Assert.Equal("Line one\nLine two\n", logged.Body);
    }

    [Fact]
    public async Task JsonOutput_ReturnsJournalEntry_WithNoTypeOrTags()
    {
        // arrange
        await InitWorkspaceAsync();
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync(
            "agent", "memory", "log", "Investigated the flaky test; still unresolved.");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var root = document.RootElement;

        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(26, root.GetProperty("id").GetString()!.Length);
        Assert.Equal("test-agent", root.GetProperty("createdBy").GetString());
        Assert.True(root.TryGetProperty("createdAt", out _));
        Assert.False(root.TryGetProperty("type", out _));
        Assert.False(root.TryGetProperty("tags", out _));
    }

    [Fact]
    public async Task TextAndFileBothMissing_ReturnsParseError()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "memory", "log");

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Contains(
            "Exactly one of the text argument or '--file' is required.", result.StdErr);
    }

    [Fact]
    public async Task NoWorkspace_ReturnsError()
    {
        // act
        var result = await ExecuteCommandAsync("agent", "memory", "log", "some text");

        // assert
        result.AssertError("No agent workspace found. Run `nitro agent init` first.");
    }

    [Fact]
    public async Task ConcurrentLogs_BothCapturesSucceed_NeitherIsLost()
    {
        // arrange
        await InitWorkspaceAsync();

        // act: two "simultaneous" log invocations racing to capture into the
        // same journal.
        var firstTask = ExecuteCommandAsync("agent", "memory", "log", "First capture.");
        var secondTask = ExecuteCommandAsync("agent", "memory", "log", "Second capture.");
        var results = await Task.WhenAll(firstTask, secondTask);

        // assert
        Assert.All(results, result => Assert.Equal(0, result.ExitCode));
        var bodies = (await ReadJournalAsync())
            .Select(entry => entry.Body)
            .OrderBy(body => body, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(["First capture.", "Second capture."], bodies);
    }
}
