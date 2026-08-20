namespace ChilliCream.Nitro.CommandLine.Commands.Mail.Options;

internal sealed class MailAllOption : Option<bool>
{
    public MailAllOption() : base("--all")
    {
        Description = "Include archived messages";
        Required = false;
    }
}
