using System.CommandLine.Help;
using ChilliCream.Nitro.CommandLine.Commands.Agent.Hook;
using ChilliCream.Nitro.CommandLine.Commands.Agent.Hooks;
using ChilliCream.Nitro.CommandLine.Commands.Agent.Mail;
using ChilliCream.Nitro.CommandLine.Commands.Agent.Memory;
using ChilliCream.Nitro.CommandLine.Commands.Agent.Tasks;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Memory;
using ChilliCream.Nitro.CommandLine.Services.Notify;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent;

internal sealed class AgentCommand : Command
{
    public AgentCommand() : base("agent")
    {
        Description = "Commands for coding agents.";

        Subcommands.Add(new InitAgentCommand());
        Subcommands.Add(new TasksCommand());
        Subcommands.Add(new MailCommand());
        Subcommands.Add(new MemoryCommand());
        Subcommands.Add(new LoginAgentCommand());
        Subcommands.Add(new RegisterAgentCommand());
        Subcommands.Add(new ListAgentCommand());
        Subcommands.Add(new HookCommand());
        Subcommands.Add(new HooksCommand());

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    /// <summary>
    /// A bare <c>nitro agent</c> invocation opens the unified tabbed TUI on
    /// the tasks tab when the terminal is interactive and an agent
    /// workspace is found; otherwise it prints the same guidance a bare
    /// group with no action would, naming <c>nitro agent init</c> among the
    /// listed subcommands. The board never takes an actor: it is an
    /// observer over the whole workspace and refuses every write.
    /// </summary>
    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();

        if (console.IsInteractive)
        {
            var taskStore = services.GetRequiredService<ITaskStore>();
            var workspaceDirectory = taskStore.FindWorkspaceDirectory();

            if (workspaceDirectory is not null)
            {
                var mailStore = services.GetRequiredService<IMailStore>();
                var memoryStore = services.GetRequiredService<IMemoryStore>();
                var agentRegistry = services.GetRequiredService<IAgentRegistry>();
                var agentSessionRegistry = services.GetRequiredService<IAgentSessionRegistry>();
                var activityReader = services.GetRequiredService<IClaudeSessionActivityReader>();
                var timeProvider = services.GetRequiredService<TimeProvider>();
                var mailWakeDaemonCoordinator = services.GetRequiredService<IMailWakeDaemonCoordinator>();
                var mailWakeReceiptObserver = services.GetRequiredService<IMailWakeReceiptObserver>();

                return await AgentTuiLauncher.RunAsync(
                    console,
                    taskStore,
                    mailStore,
                    memoryStore,
                    agentRegistry,
                    agentSessionRegistry,
                    activityReader,
                    timeProvider,
                    workspaceDirectory,
                    mailWakeDaemonCoordinator,
                    mailWakeReceiptObserver,
                    cancellationToken);
            }
        }

        return WriteBareGroupGuidance(parseResult);
    }

    /// <summary>
    /// Reproduces exactly what System.CommandLine prints today when this
    /// group is invoked bare with no action set: the "Required command was
    /// not provided." parse error on stderr, followed by the group's own
    /// help (which lists <c>init</c> among its subcommands) on stdout.
    /// Locked in so giving this group an action, needed to launch the TUI,
    /// does not silently change bare-group discoverability for
    /// non-interactive terminals or when no agent workspace is found.
    /// </summary>
    private static int WriteBareGroupGuidance(ParseResult parseResult)
    {
        parseResult.InvocationConfiguration.Error.WriteLine("Required command was not provided.");
        parseResult.InvocationConfiguration.Error.WriteLine();
        new HelpAction().Invoke(parseResult);

        return ExitCodes.Error;
    }
}
