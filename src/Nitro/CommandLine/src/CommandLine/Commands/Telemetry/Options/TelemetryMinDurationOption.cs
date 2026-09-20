namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Options;

internal sealed class TelemetryMinDurationOption : Option<int?>
{
    public TelemetryMinDurationOption() : base("--min-duration")
    {
        Description = "Only include spans lasting at least this many milliseconds";
        Required = false;
        Validators.Add(result =>
        {
            if (result.GetValue(this) is < 0)
            {
                result.AddError("Option '--min-duration' must not be negative.");
            }
        });
    }
}
