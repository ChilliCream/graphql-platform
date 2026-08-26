namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Mail.Options;

internal sealed class MailRecipientsArgument : Argument<string[]>
{
    public MailRecipientsArgument() : base("recipients")
    {
        Description = "One or more recipient agent names";
        Arity = ArgumentArity.OneOrMore;
        Validators.Add(result =>
        {
            var optionLikeRecipient = result.GetValue(this)
                ?.FirstOrDefault(recipient => recipient.StartsWith("--", StringComparison.Ordinal));

            if (optionLikeRecipient is not null)
            {
                result.AddError($"Unrecognized command or argument '{optionLikeRecipient}'.");
            }
        });
    }
}
