namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Mail.Options;

internal sealed class MailIncludeExistingOption : Option<bool>
{
    public MailIncludeExistingOption() : base("--include-existing")
    {
        Description = "Treat mail already unread at start as arrived and print it immediately, "
            + "then keep watching";
        Required = false;
    }
}
