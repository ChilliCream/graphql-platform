namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Mail.Options;

internal sealed class MailActorOption : Option<string>
{
    public MailActorOption() : base("--actor")
    {
        Description = "The actor performing this command; allocate one with `nitro agent login`";
        Required = true;
    }
}
