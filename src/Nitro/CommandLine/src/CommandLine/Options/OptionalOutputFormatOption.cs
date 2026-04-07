using ChilliCream.Nitro.CommandLine;

namespace ChilliCream.Nitro.CommandLine.Results;

internal sealed class OptionalOutputFormatOption : Option<OutputFormat?>
{
    public OptionalOutputFormatOption() : base("--output")
    {
        Description =
            "The output format (enables non-interactive mode)";

        Required = false;

        AcceptOnlyFromAmong("json");

        this.DefaultFromEnvironmentValue(EnvironmentVariables.OutputFormat);
    }
}
