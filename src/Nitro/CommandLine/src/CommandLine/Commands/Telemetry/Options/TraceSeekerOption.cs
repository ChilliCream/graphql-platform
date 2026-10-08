namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Options;

internal sealed class TraceSeekerOption : Option<string>
{
    public TraceSeekerOption() : base("--seeker")
    {
        Description = "The trace time-window cursor";
        Required = false;
        Hidden = true;
        this.NonEmptyStringsOnly();
    }
}
