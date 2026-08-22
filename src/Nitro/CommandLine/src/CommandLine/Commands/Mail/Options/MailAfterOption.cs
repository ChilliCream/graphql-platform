namespace ChilliCream.Nitro.CommandLine.Commands.Mail.Options;

internal sealed class MailAfterOption : Option<string>
{
    public const string OptionName = "--after";

    public MailAfterOption() : base(OptionName)
    {
        Description = "Deliver every message created after this cursor immediately, then keep "
            + "watching. The cursor is either an RFC 3339 timestamp or a message ID.";
        Required = false;
    }
}
