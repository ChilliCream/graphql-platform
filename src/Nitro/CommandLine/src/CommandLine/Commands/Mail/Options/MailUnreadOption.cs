namespace ChilliCream.Nitro.CommandLine.Commands.Mail.Options;

internal sealed class MailUnreadOption : Option<bool>
{
    public MailUnreadOption() : base("--unread")
    {
        Description = "Only show messages that have not been read";
        Required = false;
    }
}
