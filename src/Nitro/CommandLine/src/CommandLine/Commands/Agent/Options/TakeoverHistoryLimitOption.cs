namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Options;

internal sealed class TakeoverHistoryLimitOption : Option<int?>
{
    public TakeoverHistoryLimitOption() : base("--limit")
    {
        Description = "The maximum number of takeovers to show";
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
