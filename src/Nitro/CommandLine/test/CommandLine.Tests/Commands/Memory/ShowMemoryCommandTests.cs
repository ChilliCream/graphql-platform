namespace ChilliCream.Nitro.CommandLine.Tests.Memory;

public sealed class ShowMemoryCommandTests(NitroCommandFixture fixture)
    : MemoryCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "memory", "show", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Show a curated memory's details.

            Usage:
              nitro agent memory show <id> [options]

            Arguments:
              <id>  The memory ID

            Options:
              --output <json>  The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help   Show help and usage information

            Example:
              nitro agent memory show "01hqzxk8xdtd3fk3f0z7c5g8vm"
            """);
    }

    [Fact]
    public async Task ExistingMemory_PrintsDetails()
    {
        // arrange
        await InitWorkspaceAsync();
        var record = await SeedMemoryAsync("Use pnpm, not npm.", type: "preference", tags: ["tooling"]);

        // act
        var result = await ExecuteCommandAsync("agent", "memory", "show", record.Id);

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains($"{record.Id} (preference)", result.StdOut);
        Assert.Contains("Tags: tooling", result.StdOut);
        Assert.Contains("Use pnpm, not npm.", result.StdOut);
    }

    [Fact]
    public async Task JsonOutput_ReturnsDetailResult()
    {
        // arrange
        await InitWorkspaceAsync();
        var record = await SeedMemoryAsync("Use pnpm, not npm.", type: "preference", tags: ["tooling"]);
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "memory", "show", record.Id);

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var root = document.RootElement;

        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(record.Id, root.GetProperty("id").GetString());
        Assert.Equal("project", root.GetProperty("scope").GetString());
        Assert.Equal("preference", root.GetProperty("type").GetString());
        Assert.Equal("Use pnpm, not npm.", root.GetProperty("text").GetString());
        Assert.Equal(["tooling"], root.GetProperty("tags").EnumerateArray().Select(e => e.GetString()!).ToArray());
    }

    [Fact]
    public async Task MemoryNotFound_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync(
            "agent", "memory", "show", "01hqzxk8xdtd3fk3f0z7c5g8vm");

        // assert
        result.AssertError("Memory '01hqzxk8xdtd3fk3f0z7c5g8vm' does not exist.");
    }

    [Fact]
    public async Task NoWorkspace_ReturnsError()
    {
        // act
        var result = await ExecuteCommandAsync(
            "agent", "memory", "show", "01hqzxk8xdtd3fk3f0z7c5g8vm");

        // assert
        result.AssertError("Memory '01hqzxk8xdtd3fk3f0z7c5g8vm' does not exist.");
    }
}
