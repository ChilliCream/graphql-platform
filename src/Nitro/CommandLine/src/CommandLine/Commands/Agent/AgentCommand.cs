using System.CommandLine.Help;
using ChilliCream.Nitro.CommandLine.Commands.Agent.Hook;
using ChilliCream.Nitro.CommandLine.Commands.Agent.Hooks;
using ChilliCream.Nitro.CommandLine.Commands.Agent.Mail;
using ChilliCream.Nitro.CommandLine.Commands.Agent.Memory;
using ChilliCream.Nitro.CommandLine.Commands.Agent.Tasks;
using ChilliCream.Nitro.CommandLine.Helpers;
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
        Subcommands.Add(new TakeoverAgentCommand());
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
                var agentStore = services.GetRequiredService<IAgentStore>();
                var timeProvider = services.GetRequiredService<TimeProvider>();
                var mailWakeDaemonCoordinator = services.GetRequiredService<IMailWakeDaemonCoordinator>();

                return await AgentTuiLauncher.RunAsync(
                    console,
                    taskStore,
                    mailStore,
                    memoryStore,
                    agentRegistry,
                    agentStore,
                    timeProvider,
                    workspaceDirectory,
                    mailWakeDaemonCoordinator,
                    cancellationToken);
            }
        }

        return WriteBareGroupGuidance(parseResult);
    }

    /// <summary>
    /// Writes a missing-command error and the group help, then returns the error exit code.
    /// </summary>
    private static int WriteBareGroupGuidance(ParseResult parseResult)
    {
        parseResult.InvocationConfiguration.Error.WriteLine("Required command was not provided.");
        parseResult.InvocationConfiguration.Error.WriteLine();
        new HelpAction().Invoke(parseResult);

        return ExitCodes.Error;
    }
}
