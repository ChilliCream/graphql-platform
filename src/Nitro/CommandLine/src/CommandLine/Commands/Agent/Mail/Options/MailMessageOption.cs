namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Mail.Options;

internal sealed class MailMessageOption : Option<string>
{
    public MailMessageOption() : base("--message")
    {
        Description = "The message ID";
        Required = true;
    }
}
