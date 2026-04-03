using ChilliCream.Nitro.CommandLine;

namespace ChilliCream.Nitro.CommandLine.Results;

internal sealed class OptionalOutputFormatOption : Option<OutputFormat?>
{
    public OptionalOutputFormatOption() : base("--output")
    {
        Description =
            "The output format (enables non-interactive mode)";

        Required = false;

        // TODO: Not sure if this is better
       AcceptOnlyFromAmong("json");
        // HelpName = "json";

        this.DefaultFromEnvironmentValue(EnvironmentVariables.OutputFormat);
    }
}
