namespace ChilliCream.Nitro.CommandLine.Commands.Mail.Options;

internal sealed class MailBodyOption : Option<string>
{
    public MailBodyOption() : base("--body")
    {
        Description = "The message body. Exactly one of --body or --body-file is required";
        Required = false;
    }
}
