namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Options;

internal sealed class TelemetryLimitOption : Option<int?>
{
    public TelemetryLimitOption() : base("--limit")
    {
        Description = "The maximum number of results to show";
        Required = false;
        Validators.Add(result =>
        {
            if (result.GetValue(this) is <= 0)
            {
                result.AddError("Option '--limit' must be a positive number.");
            }
        });
    }
}
