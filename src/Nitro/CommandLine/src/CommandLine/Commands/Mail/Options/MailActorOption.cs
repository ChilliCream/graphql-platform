namespace ChilliCream.Nitro.CommandLine.Commands.Mail.Options;

internal sealed class MailActorOption : Option<string>
{
    public MailActorOption() : base("--actor")
    {
        Description = "The acting identity used on mail commands "
            + "(defaults to NITRO_MAIL_ACTOR, NITRO_TASK_ACTOR, or the OS user name)";
        Required = false;
    }
}
