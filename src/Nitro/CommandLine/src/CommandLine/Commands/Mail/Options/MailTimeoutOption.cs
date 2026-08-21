namespace ChilliCream.Nitro.CommandLine.Commands.Mail.Options;

internal sealed class MailTimeoutOption : Option<int?>
{
    public MailTimeoutOption() : base("--timeout")
    {
        Description = "Exit with an error after this many seconds if no new mail arrives "
            + "(waits until cancelled when omitted)";
        Required = false;
        Validators.Add(result =>
        {
            var timeout = result.GetValue(this);

            if (timeout is <= 0)
            {
                result.AddError("Option '--timeout' must be a positive number.");
            }
        });
    }
}
