using ChilliCream.Nitro.CommandLine.Commands.Mail;
using ChilliCream.Nitro.CommandLine.Commands.Tasks;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent;

internal sealed class AgentCommand : Command
{
    public AgentCommand() : base("agent")
    {
        Description = "Commands for coding agents.";

        Subcommands.Add(new TasksCommand());
        Subcommands.Add(new MailCommand());
    }
}
