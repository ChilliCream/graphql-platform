namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Mail.Options;

internal sealed class MailLimitOption : Option<int?>
{
    public MailLimitOption() : base("--limit")
    {
        Description = "The maximum number of messages to show";
        Required = false;
        Validators.Add(result =>
        {
            var limit = result.GetValue(this);

            if (limit is <= 0)
            {
                result.AddError("Option '--limit' must be a positive number.");
            }
        });
    }
}
