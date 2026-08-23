namespace ChilliCream.Nitro.CommandLine.Commands.Mail.Options;

internal sealed class MailNoPingOption : Option<bool>
{
    public MailNoPingOption() : base("--no-ping")
    {
        Description = "Skip the best-effort wake ping to recipients with a live claimed session";
        Required = false;
    }
}
