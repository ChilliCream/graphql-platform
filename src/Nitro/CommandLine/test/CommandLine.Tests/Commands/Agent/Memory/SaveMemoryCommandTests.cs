namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Agent.Memory;

public sealed class SaveMemoryCommandTests(NitroCommandFixture fixture)
    : MemoryCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "memory", "save", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Save a curated memory.

            Usage:
              nitro agent memory save [<text>] [options]

            Arguments:
              <text>  The memory text. Exactly one of the text argument or --file is required

            Options:
              --file <file>               A file to read the memory text from
              --type <type>               The memory type (fact, decision, preference, reference, or custom)
              --tag <tag>                 A tag; can be used multiple times
              --actor <actor> (REQUIRED)  The actor performing this command; allocate one with `nitro agent login`
              --scope <global|project>    The memory scope to write to (project or global) [default: project]
              --output <json>             The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help              Show help and usage information

            Example:
              nitro agent memory save "Use pnpm, not npm, in this repo." --type preference
              nitro agent memory save --file notes.md --type fact --tag build
            """);
    }

    [Fact]
    public async Task WithText_SavesSuccessfully()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync(
            "agent", "memory", "save", "Use pnpm, not npm.", "--type", "preference");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Single(Directory.GetFiles(CuratedDirectory, "*.md"));
    }

    [Fact]
    public async Task WithFile_ReadsText()
    {
        // arrange
        await InitWorkspaceAsync();
        var filePath = Path.Combine(WorkingDirectory, "note.md");
        await File.WriteAllTextAsync(filePath, "Line one\nLine two\n", TestContext.Current.CancellationToken);

        // act
        var result = await ExecuteCommandAsync(
            "agent", "memory", "save", "--file", "note.md", "--type", "fact");

        // assert
        Assert.Equal(0, result.ExitCode);
        var savedFile = Directory.GetFiles(CuratedDirectory, "*.md").Single();
        Assert.EndsWith("Line one\nLine two\n", await File.ReadAllTextAsync(
            savedFile, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task WithFile_NormalizesCrlfToLf()
    {
        // arrange
        await InitWorkspaceAsync();
        var filePath = Path.Combine(WorkingDirectory, "note.md");
        await File.WriteAllTextAsync(filePath, "\r\nLine one\r\nLine two\r\n", TestContext.Current.CancellationToken);

        // act
        var result = await ExecuteCommandAsync(
            "agent", "memory", "save", "--file", "note.md", "--type", "fact");

        // assert
        Assert.Equal(0, result.ExitCode);
        var savedFile = Directory.GetFiles(CuratedDirectory, "*.md").Single();
        Assert.EndsWith("Line one\nLine two\n", await File.ReadAllTextAsync(
            savedFile, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task JsonOutput_ReturnsSaveResult()
    {
        // arrange
        await InitWorkspaceAsync();
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync(
            "agent", "memory", "save", "Use pnpm, not npm.",
            "--type", "preference", "--tag", "build", "--tag", "tooling");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var root = document.RootElement;

        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(26, root.GetProperty("id").GetString()!.Length);
        Assert.Equal("project", root.GetProperty("scope").GetString());
        Assert.Equal("preference", root.GetProperty("type").GetString());
        Assert.Equal(
            ["build", "tooling"], root.GetProperty("tags").EnumerateArray().Select(e => e.GetString()!).ToArray());
        Assert.Equal("test-agent", root.GetProperty("createdBy").GetString());
        Assert.True(root.TryGetProperty("path", out _));
        Assert.True(root.TryGetProperty("createdAt", out _));
        Assert.True(root.TryGetProperty("updatedAt", out _));
    }

    [Fact]
    public async Task WithGlobalScope_SavesToGlobalStore()
    {
        // act
        var result = await ExecuteCommandAsync(
            "agent", "memory", "save", "Some global text.", "--type", "fact", "--scope", "global");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Single(Directory.GetFiles(GlobalCuratedDirectory, "*.md"));
    }

    [Fact]
    public async Task WithGlobalScope_JsonOutput_ReportsGlobalScope()
    {
        // arrange
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync(
            "agent", "memory", "save", "Some global text.", "--type", "fact", "--scope", "global");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("global", document.RootElement.GetProperty("scope").GetString());
    }

    [Fact]
    public async Task TextAndFileBothMissing_ReturnsParseError()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "memory", "save", "--type", "fact");

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Contains(
            "Exactly one of the text argument or '--file' is required.", result.StdErr);
    }

    [Fact]
    public async Task TextAndFileBothGiven_ReturnsParseError()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync(
            "agent", "memory", "save", "some text", "--file", "notes.txt", "--type", "fact");

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Contains(
            "Exactly one of the text argument or '--file' is required.", result.StdErr);
    }

    [Fact]
    public async Task TypeMissing_ReturnsParseError()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "memory", "save", "some text");

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Contains("Option '--type' is required.", result.StdErr);
    }

    [Fact]
    public async Task InvalidType_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync(
            "agent", "memory", "save", "some text", "--type", "Not Valid!");

        // assert
        result.AssertError(
            "The type 'Not Valid!' is invalid. A type may contain only lowercase letters, digits, "
            + "and hyphens, up to 40 characters.");
    }

    [Fact]
    public async Task InvalidTag_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync(
            "agent", "memory", "save", "some text", "--type", "fact", "--tag", "Not Valid!");

        // assert
        result.AssertError(
            "The tag 'Not Valid!' is invalid. A tag may contain only lowercase letters, digits, "
            + "and hyphens, up to 40 characters.");
    }

    [Fact]
    public async Task EmptyTextArgument_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "memory", "save", "", "--type", "fact");

        // assert
        result.AssertError("The text argument must not be empty.");
    }

    [Fact]
    public async Task FileDoesNotExist_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync(
            "agent", "memory", "save", "--file", "missing.txt", "--type", "fact");

        // assert
        result.AssertError("The file 'missing.txt' does not exist.");
    }

    [Fact]
    public async Task NoWorkspace_ReturnsError()
    {
        // act
        var result = await ExecuteCommandAsync("agent", "memory", "save", "some text", "--type", "fact");

        // assert
        result.AssertError("No agent workspace found. Run `nitro agent init` first.");
    }
}
