namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Mail.Options;

internal sealed class MailFromOption : Option<string>
{
    public MailFromOption() : base("--from")
    {
        Description = "Only show messages sent by this agent";
        Required = false;
    }
}
