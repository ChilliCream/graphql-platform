using ChilliCream.Nitro.CommandLine.Services.Memory;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Covers the actor guard across the command surface. Actor names are
/// allocated, never invented, so every command taking <c>--actor</c>
/// rejects a name no allocation ever minted, and accepts one
/// <c>agent login</c> did.
/// </summary>
public sealed class ActorGuardTests : AgentCommandTestBase
{
    private MemoryStore CreateStore()
        => new(new TestFileSystem(WorkingDirectory), FakeTime, new AgentDatabase());

    public ActorGuardTests(NitroCommandFixture fixture) : base(fixture)
    {
        // The guard lives in the real resolver, so this suite must not run
        // behind the fixed one every other command test uses.
        SetupRealActingActor();
        DefaultActor = null;
    }

    [Theory]
    [InlineData("tasks", "create", "Fix the parser")]
    [InlineData("tasks", "comment", "add", "acme-1a2", "Looks good to me.")]
    [InlineData("tasks", "close", "acme-1a2")]
    [InlineData("mail", "send", "--body", "All good.", "--to", "maya", "--subject", "Status")]
    [InlineData("memory", "save", "Use pnpm, not npm.", "--type", "preference")]
    public async Task Command_Should_Fail_When_TheActorWasNeverAllocated(params string[] command)
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync(["agent", .. command, "--actor", "never-allocated"]);

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Contains("Unknown actor 'never-allocated'", result.StdErr);
    }

    [Fact]
    public async Task TaskCommands_Should_Succeed_When_TheActorWasAllocatedByLogin()
    {
        // arrange
        await InitWorkspaceAsync();
        var actor = await LoginAsync();
        var create = await ExecuteCommandAsync(
            "agent", "tasks", "create", "Fix the parser", "--actor", actor);
        var id = await QueryScalarAsync("SELECT id FROM tasks");

        // act
        var comment = await ExecuteCommandAsync(
            "agent", "tasks", "comment", "add", id!, "Looks good to me.", "--actor", actor);
        var close = await ExecuteCommandAsync("agent", "tasks", "close", id!, "--actor", actor);

        // assert
        Assert.Equal(0, create.ExitCode);
        Assert.Equal(0, comment.ExitCode);
        Assert.Equal(0, close.ExitCode);
        Assert.Equal(actor, await QueryScalarAsync($"SELECT author FROM comments WHERE task_id = '{id}'"));
    }

    [Fact]
    public async Task MailSend_Should_Succeed_When_TheActorWasAllocatedByLogin()
    {
        // arrange
        await InitWorkspaceAsync();
        var actor = await LoginAsync();
        await SeedAgentAsync("ada");

        // act
        var result = await ExecuteCommandAsync(
            "agent", "mail", "send", "--body", "All good.", "--to", "ada", "--subject", "Status", "--actor", actor);

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(actor, await QueryScalarAsync("SELECT sender FROM messages WHERE subject = 'Status'"));
    }

    [Fact]
    public async Task MemorySave_Should_Succeed_When_TheActorWasAllocatedByLogin()
    {
        // arrange
        await InitWorkspaceAsync();
        var actor = await LoginAsync();

        // act
        var result = await ExecuteCommandAsync(
            "agent", "memory", "save", "Use pnpm, not npm.", "--type", "preference", "--actor", actor);

        // assert
        Assert.Equal(0, result.ExitCode);
        var saved = Assert.Single(await CreateStore().GetRecentCuratedAsync(
            limit: null, TestContext.Current.CancellationToken));
        Assert.Equal(actor, saved.CreatedBy);
    }

    /// <summary>
    /// Allocates an actor the way a harness without a session-start hook
    /// does, and returns the minted name.
    /// </summary>
    private async Task<string> LoginAsync()
    {
        var result = await ExecuteCommandAsync("agent", "login");
        Assert.Equal(0, result.ExitCode);

        return (await QueryScalarAsync("SELECT name FROM agents"))!;
    }
}
