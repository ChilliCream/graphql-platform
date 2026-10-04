namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Mail.Options;

internal sealed class MailMessagesOption : Option<string[]>
{
    public MailMessagesOption() : base("--message")
    {
        Description = "A message ID; repeat for several messages";
        Required = true;
        AllowMultipleArgumentsPerToken = true;
        Arity = ArgumentArity.OneOrMore;
    }
}
