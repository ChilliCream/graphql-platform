namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Mail.Options;

internal sealed class MailThreadOption : Option<bool>
{
    public MailThreadOption() : base("--thread")
    {
        Description = "Print every message of the thread, oldest first, and mark them all read";
        Required = false;
    }
}
