namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Session;

internal sealed class SessionCommand : Command
{
    public SessionCommand() : base("session")
    {
        Description = "Manage this workspace's live harness session presence.";

        Subcommands.Add(new ClaimSessionCommand());
        Subcommands.Add(new ListSessionCommand());
        Subcommands.Add(new StatusSessionCommand());
    }
}
