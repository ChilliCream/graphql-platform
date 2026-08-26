namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Mail.Options;

internal sealed class MailMessageOption : Option<string>
{
    public MailMessageOption() : base("--message")
    {
        Description = "The ID of the message to reply to";
        Required = true;
    }
}
