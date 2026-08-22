namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Only covers command wiring (help text) at this layer. The claim state
/// machine and the self-claim bootstrap are exercised directly against
/// <see cref="ChilliCream.Nitro.CommandLine.Services.Workspace.AgentSessionRegistry"/>
/// in <c>AgentSessionRegistryTests</c>, with a fixed ancestor resolver:
/// this command's own ancestor-walk depends on the REAL process tree it
/// runs under, which the test host cannot control (and, running nested
/// inside a live Claude Code session, cannot even predict deterministically).
/// </summary>
public sealed class ClaimSessionCommandTests(NitroCommandFixture fixture)
    : AgentCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "session", "claim", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Bind the resolved actor to this process's harness session.

            Usage:
              nitro agent session claim [options]

            Options:
              --actor <actor>  The acting identity used on mail commands (defaults to NITRO_MAIL_ACTOR, NITRO_TASK_ACTOR, or the OS user name)
              --force-rebind   Reclaim a session already explicitly claimed by a different actor, resetting its delivery ledger and block budget
              --output <json>  The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help   Show help and usage information

            Example:
              nitro agent session claim
              nitro agent session claim --actor codex --force-rebind
            """);
    }
}
