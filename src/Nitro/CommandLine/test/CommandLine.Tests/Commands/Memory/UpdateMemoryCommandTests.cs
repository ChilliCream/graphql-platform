namespace ChilliCream.Nitro.CommandLine.Tests.Memory;

public sealed class UpdateMemoryCommandTests(NitroCommandFixture fixture)
    : MemoryCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "memory", "update", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Update a curated memory's fields.

            Usage:
              nitro agent memory update <id> [options]

            Arguments:
              <id>  The memory ID

            Options:
              --text <text>              The new memory text. At most one of --text or --file may be given
              --file <file>              A file to read the memory text from. Exactly one of the text argument or --file is required
              --type <type>              The memory type (fact, decision, preference, reference, or custom)
              --add-tag <add-tag>        A tag to add; can be used multiple times
              --remove-tag <remove-tag>  A tag to remove; can be used multiple times
              --output <json>            The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help             Show help and usage information

            Example:
              nitro agent memory update "01hqzxk8xdtd3fk3f0z7c5g8vm" --type decision
              nitro agent memory update "01hqzxk8xdtd3fk3f0z7c5g8vm" --add-tag api --remove-tag draft
            """);
    }

    [Fact]
    public async Task NoOptionsGiven_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();
        var record = await SeedMemoryAsync("Original text.");

        // act
        var result = await ExecuteCommandAsync("agent", "memory", "update", record.Id);

        // assert
        result.AssertError("Nothing to update. Pass at least one option.");
    }

    [Fact]
    public async Task WithType_UpdatesType()
    {
        // arrange
        await InitWorkspaceAsync();
        var record = await SeedMemoryAsync("Original text.");

        // act
        var result = await ExecuteCommandAsync(
            "agent", "memory", "update", record.Id, "--type", "decision");

        // assert
        result.AssertSuccess($"✓ Updated memory '{record.Id}'.");
        var content = await File.ReadAllTextAsync(record.Path, TestContext.Current.CancellationToken);
        Assert.Contains("type: decision", content);
    }

    [Fact]
    public async Task WithText_ReplacesBody()
    {
        // arrange
        await InitWorkspaceAsync();
        var record = await SeedMemoryAsync("Original text.");

        // act
        var result = await ExecuteCommandAsync(
            "agent", "memory", "update", record.Id, "--text", "New text.");

        // assert
        result.AssertSuccess($"✓ Updated memory '{record.Id}'.");
        var content = await File.ReadAllTextAsync(record.Path, TestContext.Current.CancellationToken);
        Assert.EndsWith("New text.", content);
    }

    [Fact]
    public async Task WithAddAndRemoveTag_ChangesTags()
    {
        // arrange
        await InitWorkspaceAsync();
        var record = await SeedMemoryAsync("Original text.", tags: ["draft"]);

        // act
        var result = await ExecuteCommandAsync(
            "agent", "memory", "update", record.Id, "--add-tag", "api", "--remove-tag", "draft");
        SetupInteractionMode(InteractionMode.JsonOutput);
        var showResult = await ExecuteCommandAsync("agent", "memory", "show", record.Id);

        // assert
        Assert.Equal(0, result.ExitCode);
        using var document = System.Text.Json.JsonDocument.Parse(showResult.StdOut);
        var tags = document.RootElement.GetProperty("tags").EnumerateArray().Select(e => e.GetString()!).ToArray();
        Assert.Equal(["api"], tags);
    }

    [Fact]
    public async Task TextAndFileBothGiven_ReturnsParseError()
    {
        // arrange
        await InitWorkspaceAsync();
        var record = await SeedMemoryAsync("Original text.");

        // act
        var result = await ExecuteCommandAsync(
            "agent", "memory", "update", record.Id, "--text", "x", "--file", "notes.txt");

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Contains("At most one of '--text' or '--file' may be given.", result.StdErr);
    }

    [Fact]
    public async Task MemoryNotFound_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync(
            "agent", "memory", "update", "01hqzxk8xdtd3fk3f0z7c5g8vm", "--type", "decision");

        // assert
        result.AssertError("Memory '01hqzxk8xdtd3fk3f0z7c5g8vm' does not exist.");
    }
}
