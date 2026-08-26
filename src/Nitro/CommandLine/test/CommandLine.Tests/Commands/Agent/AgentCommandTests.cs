using ChilliCream.Nitro.CommandLine.Tests.Commands.Agent.Tasks;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Agent;

/// <summary>
/// Covers the bare <c>nitro agent</c> entry point: the group command now
/// carries its own action (launching the unified tabbed TUI) alongside its
/// subcommands, so this locks in that the group's help and its subcommand
/// dispatch stay exactly as they were before the action was added, and that
/// the paths which do not launch the TUI (non-interactive, or interactive
/// with no workspace) fall back to the same guidance a bare group with no
/// action prints.
/// </summary>
public sealed class AgentCommandTests(NitroCommandFixture fixture)
    : TasksCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Commands for coding agents.

            Usage:
              nitro agent [command] [options]

            Options:
              -?, -h, --help  Show help and usage information

            Commands:
              init      Initialize an agent workspace in the current directory.
              tasks     Create and manage tasks for coding agents.
              mail      Send and receive mail between coding agents.
              memory    Save and recall durable agent memory.
              login     Allocate an actor name for a harness without a session-start hook.
              register  Set the role of an actor allocated by `agent login` or a session-start hook.
              list      List the actors this workspace knows, with their session when they have one.
              hooks     Install, inspect, and remove Nitro's turn-boundary hook entries per harness.
            """);
    }

    [Fact]
    public async Task Bare_NonInteractive_PrintsBareGroupGuidance()
    {
        // arrange & act: no workspace either, but non-interactive alone is
        // enough to skip straight to the guidance without resolving one.
        var result = await ExecuteCommandAsync("agent");

        // assert
        result.AssertBareGroupGuidance(
            """
            Description:
              Commands for coding agents.

            Usage:
              nitro agent [command] [options]

            Options:
              -?, -h, --help  Show help and usage information

            Commands:
              init      Initialize an agent workspace in the current directory.
              tasks     Create and manage tasks for coding agents.
              mail      Send and receive mail between coding agents.
              memory    Save and recall durable agent memory.
              login     Allocate an actor name for a harness without a session-start hook.
              register  Ensure the current session has an actor, or update its actor and role.
              list      List current actors and their durable harness sessions.
              hooks     Install, inspect, and remove Nitro's turn-boundary hook entries per harness.
            """);
    }

    [Fact]
    public async Task Bare_NonInteractive_WithWorkspace_StillPrintsBareGroupGuidance()
    {
        // arrange: a workspace exists, but non-interactive never gets far
        // enough to check for one, so the TUI is never launched.
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent");

        // assert
        result.AssertBareGroupGuidance(
            """
            Description:
              Commands for coding agents.

            Usage:
              nitro agent [command] [options]

            Options:
              -?, -h, --help  Show help and usage information

            Commands:
              init      Initialize an agent workspace in the current directory.
              tasks     Create and manage tasks for coding agents.
              mail      Send and receive mail between coding agents.
              memory    Save and recall durable agent memory.
              login     Allocate an actor name for a harness without a session-start hook.
              register  Ensure the current session has an actor, or update its actor and role.
              list      List current actors and their durable harness sessions.
              hooks     Install, inspect, and remove Nitro's turn-boundary hook entries per harness.
            """);
    }

    [Fact]
    public async Task Bare_Interactive_NoWorkspace_PrintsBareGroupGuidance()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);

        // act
        var result = await ExecuteCommandAsync("agent");

        // assert: the guidance's subcommand list already names `init`, so
        // no separate no-workspace message is needed.
        result.AssertBareGroupGuidance(
            """
            Description:
              Commands for coding agents.

            Usage:
              nitro agent [command] [options]

            Options:
              -?, -h, --help  Show help and usage information

            Commands:
              init      Initialize an agent workspace in the current directory.
              tasks     Create and manage tasks for coding agents.
              mail      Send and receive mail between coding agents.
              memory    Save and recall durable agent memory.
              login     Allocate an actor name for a harness without a session-start hook.
              register  Ensure the current session has an actor, or update its actor and role.
              list      List current actors and their durable harness sessions.
              hooks     Install, inspect, and remove Nitro's turn-boundary hook entries per harness.
            """);
    }

    [Theory]
    [InlineData("tasks")]
    [InlineData("mail")]
    public async Task StandaloneBoardCommand_Should_NotBeAvailable(string command)
    {
        // act
        var result = await ExecuteCommandAsync("agent", command, "board");

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Contains(
            "Unrecognized command or argument 'board'.",
            result.StdOut + result.StdErr,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Dispatch_Should_StillReachTheInitLeaf_NotTheGroupAction()
    {
        // act: `init` is itself a direct subcommand of `agent`, the
        // shallowest possible case for the group action being bypassed.
        var result = await ExecuteCommandAsync("agent", "init");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Initialized agent workspace", result.StdOut);
    }
}
