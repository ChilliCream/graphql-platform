namespace ChilliCream.Nitro.CommandLine.Commands.Mail;

internal sealed class MailCommand : Command
{
    public MailCommand() : base("mail")
    {
        Description = "Send and receive mail between coding agents.";

        Subcommands.Add(new InitMailCommand());
        Subcommands.Add(new RegisterMailCommand());
        Subcommands.Add(new WhoamiMailCommand());
        Subcommands.Add(new AgentsMailCommand());
    }
}
