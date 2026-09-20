using ChilliCream.Nitro.CommandLine.Results;

namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Options;

internal sealed class TelemetryOutputFormatOption : Option<OutputFormat?>
{
    public TelemetryOutputFormatOption() : base("--output")
    {
        Description = "The output format (enables non-interactive mode)";
        Required = false;
        AcceptOnlyFromAmong("json", "ndjson");
        this.DefaultFromEnvironmentValue(EnvironmentVariables.OutputFormat);
    }
}
