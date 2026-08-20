using System.CommandLine.Help;
using ChilliCream.Nitro.CommandLine.Commands.Mail;
using ChilliCream.Nitro.CommandLine.Commands.Tasks;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Tasks;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent;

internal sealed class AgentCommand : Command
{
    public AgentCommand() : base("agent")
    {
        Description = "Commands for coding agents.";

        Subcommands.Add(new InitAgentCommand());
        Subcommands.Add(new TasksCommand());
        Subcommands.Add(new MailCommand());

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    /// <summary>
    /// A bare <c>nitro agent</c> invocation opens the unified tabbed TUI on
    /// the tasks tab when the terminal is interactive and an agent
    /// workspace is found; otherwise it prints the same guidance a bare
    /// group with no action would, naming <c>nitro agent init</c> among the
    /// listed subcommands.
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
                var timeProvider = services.GetRequiredService<TimeProvider>();
                var environmentVariableProvider = services.GetRequiredService<IEnvironmentVariableProvider>();

                return await AgentTuiLauncher.RunAsync(
                    console,
                    taskStore,
                    mailStore,
                    timeProvider,
                    environmentVariableProvider,
                    workspaceDirectory,
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
