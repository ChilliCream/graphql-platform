namespace ChilliCream.Nitro.CommandLine.Commands.Mail;

internal sealed class MailCommand : Command
{
    public MailCommand() : base("mail")
    {
        Description = "Send and receive mail between coding agents.";

        Subcommands.Add(new SendMailCommand());
        Subcommands.Add(new ReplyMailCommand());
        Subcommands.Add(new BroadcastMailCommand());
        Subcommands.Add(new InboxMailCommand());
        Subcommands.Add(new ReadMailCommand());
        Subcommands.Add(new AckMailCommand());
        Subcommands.Add(new ArchiveMailCommand());
        Subcommands.Add(new ThreadsMailCommand());
        Subcommands.Add(new SearchMailCommand());
        Subcommands.Add(new WatchMailCommand());
    }
}
