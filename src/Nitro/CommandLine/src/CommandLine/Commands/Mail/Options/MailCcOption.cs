namespace ChilliCream.Nitro.CommandLine.Commands.Mail.Options;

internal sealed class MailCcOption : Option<string[]>
{
    public MailCcOption() : base("--cc")
    {
        Description = "A recipient to carbon-copy; can be used multiple times";
        Required = false;
    }
}
