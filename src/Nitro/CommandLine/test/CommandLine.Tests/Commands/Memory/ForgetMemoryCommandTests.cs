namespace ChilliCream.Nitro.CommandLine.Tests.Memory;

public sealed class ForgetMemoryCommandTests(NitroCommandFixture fixture)
    : MemoryCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "memory", "forget", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Permanently delete a curated memory. This is a hard delete: the markdown file and its index entry are removed, with no tombstone. Git history is not erased, so a merge or checkout can resurrect the deleted content; forget is therefore not a privacy-erasure guarantee.

            Usage:
              nitro agent memory forget <id> [options]

            Arguments:
              <id>  The memory ID

            Options:
              --force          Skip confirmation prompts for deletes and overwrites
              --output <json>  The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help   Show help and usage information

            Example:
              nitro agent memory forget "01hqzxk8xdtd3fk3f0z7c5g8vm"
              nitro agent memory forget "01hqzxk8xdtd3fk3f0z7c5g8vm" --force
            """);
    }

    [Fact]
    public async Task WithForce_DeletesFileAndIsHardDelete()
    {
        // arrange
        await InitWorkspaceAsync();
        var record = await SeedMemoryAsync("Original text.");

        // act
        var result = await ExecuteCommandAsync("agent", "memory", "forget", record.Id, "--force");

        // assert
        result.AssertSuccess($"✓ Deleted memory '{record.Id}'.");
        Assert.False(File.Exists(record.Path));
        Assert.Empty(Directory.GetFiles(CuratedDirectory, "*.md"));
    }

    [Fact]
    public async Task WithoutForce_NonInteractive_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();
        var record = await SeedMemoryAsync("Original text.");

        // act
        var result = await ExecuteCommandAsync("agent", "memory", "forget", record.Id);

        // assert
        result.AssertError("Use --force to delete without confirmation.");
        Assert.True(File.Exists(record.Path));
    }

    [Fact]
    public async Task WithoutForce_Interactive_Declined_Aborts()
    {
        // arrange
        await InitWorkspaceAsync();
        var record = await SeedMemoryAsync("Original text.");
        SetupInteractionMode(InteractionMode.Interactive);

        var command = StartInteractiveCommand("agent", "memory", "forget", record.Id);

        // act
        command.Confirm(false);
        var result = await command.RunToCompletionAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Aborted.", result.StdOut);
        Assert.True(File.Exists(record.Path));
    }

    [Fact]
    public async Task MemoryNotFound_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync(
            "agent", "memory", "forget", "01hqzxk8xdtd3fk3f0z7c5g8vm", "--force");

        // assert
        result.AssertError("Memory '01hqzxk8xdtd3fk3f0z7c5g8vm' does not exist.");
    }
}
