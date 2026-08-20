namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Tasks;

/// <summary>
/// Proves the .1 boundary end to end through the real binary surface: the
/// retargeted <c>agent tasks init</c> and <c>agent mail init</c> commands
/// build the SAME unified workspace, and task and mail commands both
/// operate against that one file.
/// </summary>
public sealed class UnifiedWorkspaceEndToEndTests(NitroCommandFixture fixture)
    : TasksCommandTestBase(fixture)
{
    [Fact]
    public async Task TasksInit_ThenMailInit_Should_ShareTheSameUnifiedWorkspace()
    {
        // arrange
        await InitWorkspaceAsync();
        var taskId = await CreateTaskAsync("Ship the unified workspace");

        // act: the retargeted mail init sees the same already-initialized
        // path, proving there is only one workspace, not two.
        var mailInitAgain = await ExecuteCommandAsync("agent", "mail", "init");

        // assert
        mailInitAgain.AssertError(
            "Already initialized at '.nitro/agents'. Use --force to reinitialize.");

        var registerResult = await ExecuteCommandAsync("agent", "mail", "register", "--actor", "bob");
        Assert.Equal(0, registerResult.ExitCode);

        var sendResult = await ExecuteCommandAsync(
            "agent", "mail", "send", "bob", "--subject", "Status", "--body", "Merged.");
        Assert.Equal(0, sendResult.ExitCode);

        // Both features' rows live in the one database file this bead
        // unifies onto.
        Assert.Equal(
            "2",
            await QueryScalarAsync("PRAGMA user_version;"));
        Assert.Equal(
            "1",
            await QueryScalarAsync("SELECT COUNT(*) FROM tasks"));
        Assert.Equal(
            "1",
            await QueryScalarAsync("SELECT COUNT(*) FROM messages"));

        var listResult = await ExecuteCommandAsync("agent", "tasks", "list");
        Assert.Contains(taskId, listResult.StdOut);
    }
}
