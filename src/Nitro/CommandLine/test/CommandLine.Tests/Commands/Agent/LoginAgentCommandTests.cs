using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Covers <c>agent login</c>: allocating an actor name for a harness with no
/// session-start hook, so it can be bound with <c>agent register --actor</c>.
/// </summary>
public sealed class LoginAgentCommandTests(NitroCommandFixture fixture) : AgentCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "login", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Allocate an actor name for a harness without a session-start hook.

            Usage:
              nitro agent login [options]

            Options:
              --output <json>  The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help   Show help and usage information

            Example:
              nitro agent login
            """);
    }

    [Fact]
    public async Task Login_Should_AllocateAnActor_AndTellHowToBindIt()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "login");

        // assert
        Assert.Equal(0, result.ExitCode);
        var actor = await QueryScalarAsync("SELECT name FROM agents");
        Assert.NotNull(actor);
        Assert.Equal(
            $"""
            ✓ Your Nitro actor is '{actor}'.

            Bind it to this session with:
              nitro agent register --actor {actor}
            """,
            result.StdOut.Trim());
    }

    [Fact]
    public async Task Login_Should_BindNoSession()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "login");

        // assert: the name exists but belongs to nobody until register binds it.
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("1", await QueryScalarAsync("SELECT COUNT(*) FROM agents"));
        Assert.Equal("0", await QueryScalarAsync("SELECT COUNT(*) FROM agent_session_identities"));
        Assert.Equal("0", await QueryScalarAsync("SELECT COUNT(*) FROM agent_sessions"));
    }

    [Fact]
    public async Task Login_Should_AllocateADistinctActor_OnEveryCall()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        await ExecuteCommandAsync("agent", "login");
        await ExecuteCommandAsync("agent", "login");

        // assert
        Assert.Equal("2", await QueryScalarAsync("SELECT COUNT(DISTINCT name) FROM agents"));
    }

    [Fact]
    public async Task JsonOutput_Should_ReturnTheActor()
    {
        // arrange
        await InitWorkspaceAsync();
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "login");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var actor = await QueryScalarAsync("SELECT name FROM agents");

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(actor, document.RootElement.GetProperty("actor").GetString());
    }

    [Fact]
    public async Task Login_Should_AllocateAnActor_ThatRegisterAccepts()
    {
        // arrange: the login name is the only thing `register --actor` will
        // now accept, since actor names are allocated and never invented.
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "login");
        var actor = await QueryScalarAsync("SELECT name FROM agents");

        // act
        var result = await ExecuteCommandAsync("agent", "register", "--actor", actor!);

        // assert: registering the allocated name needs no session of any kind.
        Assert.Equal(0, result.ExitCode);
        Assert.Equal($"✓ Actor '{actor}'.", result.StdOut.Trim());
        Assert.Equal("0", await QueryScalarAsync("SELECT COUNT(*) FROM agent_session_identities"));
        Assert.Equal("0", await QueryScalarAsync("SELECT COUNT(*) FROM agent_sessions"));
    }

    [Fact]
    public async Task Login_Should_AllocateAnActor_ThatActsWithoutASession()
    {
        // arrange: no harness session at all, only the allocated name.
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "login");
        var actor = await QueryScalarAsync("SELECT name FROM agents");

        // act
        var result = await ExecuteCommandAsync(
            "agent", "tasks", "create", "Fix the parser", "--actor", actor!);

        // assert: a session is only ever needed to push mail, never to act.
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(
            actor,
            await QueryScalarAsync("SELECT actor FROM events WHERE event_type = 'created'"));
        Assert.Equal("0", await QueryScalarAsync("SELECT COUNT(*) FROM agent_sessions"));
    }

    [Fact]
    public async Task Register_Should_Fail_When_TheActorWasNeverAllocated()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "register", "--actor", "never-allocated");

        // assert: a name nothing allocated is rejected, and never created.
        result.AssertError(
            "Unknown actor 'never-allocated'. Run `nitro agent login` to allocate one, or `nitro agent list` to see the actors this workspace knows.");
        Assert.Equal("0", await QueryScalarAsync("SELECT COUNT(*) FROM agents"));
    }

    [Fact]
    public async Task Register_Should_Fail_When_TheActorWasNeverAllocated_AndASessionIsLive()
    {
        // arrange: a live Claude session mints its own actor, which still
        // does not make an unrelated name usable.
        await InitWorkspaceAsync();
        SetupAncestorSessionResolvers(
            claude: new ClaudeAncestorSession(Environment.ProcessId, "session-1", WorkingDirectory, ""));
        await ExecuteCommandAsync("agent", "hook", "claude", "session-start");

        // act
        var result = await ExecuteCommandAsync("agent", "register", "--actor", "never-allocated");

        // assert
        result.AssertError(
            "Unknown actor 'never-allocated'. Run `nitro agent login` to allocate one, or `nitro agent list` to see the actors this workspace knows.");
    }
}
