namespace ChilliCream.Nitro.CommandLine.Commands.Mail.Options;

internal sealed class MailSubjectOption : Option<string>
{
    public MailSubjectOption() : base("--subject")
    {
        Description = "The message subject";
        Required = true;
    }
}
