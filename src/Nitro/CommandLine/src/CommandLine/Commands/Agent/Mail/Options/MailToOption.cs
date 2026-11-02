namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Mail.Options;

internal sealed class MailToOption : Option<string[]>
{
    public MailToOption() : base("--to")
    {
        Description = "A recipient agent name; repeat for several recipients";
        Required = true;
        AllowMultipleArgumentsPerToken = true;
        Arity = ArgumentArity.OneOrMore;
    }
}
