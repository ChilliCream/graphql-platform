namespace ChilliCream.Nitro.CommandLine.Commands.Mail.Options;

internal sealed class MailRecipientsArgument : Argument<string[]>
{
    public MailRecipientsArgument() : base("recipients")
    {
        Description = "One or more recipient agent names";
        Arity = ArgumentArity.OneOrMore;
    }
}
