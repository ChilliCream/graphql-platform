namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Options;

internal sealed class TraceSpanOption : Option<string>
{
    public TraceSpanOption() : base("--span")
    {
        Description = "Focus on the subtree rooted at a span ID";
        Required = false;
        this.NonEmptyStringsOnly();
    }
}
