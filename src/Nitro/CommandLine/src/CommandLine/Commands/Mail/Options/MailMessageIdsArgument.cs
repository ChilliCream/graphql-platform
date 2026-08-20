namespace ChilliCream.Nitro.CommandLine.Commands.Mail.Options;

internal sealed class MailMessageIdsArgument : Argument<string[]>
{
    public MailMessageIdsArgument() : base("message-ids")
    {
        Description = "One or more message IDs";
        Arity = ArgumentArity.OneOrMore;
    }
}
