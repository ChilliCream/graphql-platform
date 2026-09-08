using System.Text.Json;
using ChilliCream.Nitro.CommandLine.Tests.Commands;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

public sealed class RegisterAgentCommandTests(NitroCommandFixture fixture)
    : AgentCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSimplifiedOptions()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "register", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Set the role of an actor allocated by `agent login` or a session-start hook.

            Usage:
              nitro agent register [options]

            Options:
              --actor <actor> (REQUIRED)  The actor to register; allocate one with `nitro agent login`
              --role <role>               The actor role, normalized lowercase. Known roles: orchestrator, planner, implementer, reviewer, researcher; any other value is accepted.
              --output <json>             The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help              Show help and usage information

            Example:
              nitro agent register --actor "maya"
              nitro agent register --actor "maya" --role "researcher"
            """);
    }

    [Fact]
    public async Task Register_Should_SetTheRole_When_TheActorWasAllocated()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("maya");

        // act
        var result = await ExecuteCommandAsync("agent", "register", "--actor", "maya", "--role", "Backend");

        // assert
        result.AssertSuccess("✓ Actor 'maya', role 'backend'.");
        Assert.Equal("backend", await QueryScalarAsync("SELECT role FROM agents WHERE name = 'maya'"));
    }

    [Fact]
    public async Task Register_Should_ReportTheActorAlone_When_NoRoleIsGiven()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("maya");

        // act
        var result = await ExecuteCommandAsync("agent", "register", "--actor", "maya");

        // assert
        result.AssertSuccess("✓ Actor 'maya'.");
        Assert.Equal("", await QueryScalarAsync("SELECT role FROM agents WHERE name = 'maya'"));
    }

    [Fact]
    public async Task Register_Should_ClearTheRole_When_TheRoleIsExplicitlyEmpty()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("maya");
        await ExecuteCommandAsync("agent", "register", "--actor", "maya", "--role", "planner");

        // act
        var result = await ExecuteCommandAsync("agent", "register", "--actor", "maya", "--role", "");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("", await QueryScalarAsync("SELECT role FROM agents WHERE name = 'maya'"));
    }

    [Fact]
    public async Task Register_Should_Fail_When_TheActorWasNeverAllocated()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "register", "--actor", "maya");

        // assert
        result.AssertError(
            "Unknown actor 'maya'. Run `nitro agent login` to allocate one, "
            + "or `nitro agent list` to see the actors this workspace knows.");
        Assert.Equal("0", await QueryScalarAsync("SELECT COUNT(*) FROM agents"));
    }

    [Fact]
    public async Task Register_Should_Fail_When_ActorIsOmitted()
    {
        // arrange: no default actor is supplied for this one, so the parser
        // sees the command exactly as a caller who omitted it would.
        await InitWorkspaceAsync();
        DefaultActor = null;

        // act
        var result = await ExecuteCommandAsync("agent", "register");

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Contains("Option '--actor' is required", result.StdErr);
    }

    [Fact]
    public async Task JsonOutput_Should_CarryActorAndRole()
    {
        // arrange
        await InitWorkspaceAsync();
        await SeedAgentAsync("maya");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "register", "--actor", "maya", "--role", "worker");

        // assert
        using var document = JsonDocument.Parse(result.StdOut);
        var root = document.RootElement;
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("maya", root.GetProperty("actor").GetString());
        Assert.Equal("worker", root.GetProperty("role").GetString());
    }

    [Theory]
    [InlineData("--client")]
    [InlineData("--force")]
    [InlineData("--force-rebind")]
    public async Task Register_RemovedOptionsAreRejected(string option)
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "register", option, "value");

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Contains($"Unrecognized command or argument '{option}'", result.StdErr);
    }
}
