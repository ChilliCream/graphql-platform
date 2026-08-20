namespace ChilliCream.Nitro.CommandLine.Commands.Mail.Options;

internal sealed class MailSearchTextArgument : Argument<string>
{
    public MailSearchTextArgument() : base("text")
    {
        Description = "The text to search for in the subject and body";
    }
}
