namespace ChilliCream.Nitro.CommandLine.Commands.Mail.Options;

internal sealed class MailActorOption : Option<string>
{
    public MailActorOption() : base("--actor")
    {
        Description = "The actor performing this command; inferred from the current session when omitted";
        Required = false;
    }
}
