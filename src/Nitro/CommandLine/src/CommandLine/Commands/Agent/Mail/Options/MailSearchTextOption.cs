namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Mail.Options;

internal sealed class MailSearchTextOption : Option<string>
{
    public MailSearchTextOption() : base("--text")
    {
        Description = "The text to search for in the subject, body, and sender";
        Required = true;
    }
}
