namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Mail.Options;

internal sealed class MailBodyOption : Option<string>
{
    public MailBodyOption() : base("--body")
    {
        Description = "The message body; use --body-file to read it from a file instead";
        Required = false;
    }
}
