namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Mail.Options;

internal sealed class MailBodyFileOption : Option<string>
{
    public MailBodyFileOption() : base("--body-file")
    {
        Description = "A file to read the message body from; use it instead of --body";
        Required = false;
    }
}
