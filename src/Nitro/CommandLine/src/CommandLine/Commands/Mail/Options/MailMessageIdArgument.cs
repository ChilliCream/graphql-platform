namespace ChilliCream.Nitro.CommandLine.Commands.Mail.Options;

internal sealed class MailMessageIdArgument : Argument<string>
{
    public MailMessageIdArgument() : base("message-id")
    {
        Description = "The message ID";
    }
}
