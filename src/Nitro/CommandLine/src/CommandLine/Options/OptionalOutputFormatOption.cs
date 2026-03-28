using ChilliCream.Nitro.CommandLine.Options;

namespace ChilliCream.Nitro.CommandLine.Results;

internal sealed class OptionalOutputFormatOption : Option<OutputFormat?>
{
    public OptionalOutputFormatOption() : base("--output")
    {
        Description =
            "The format in which the result should be displayed, if this option is set, the console will be non-interactive and the result will be displayed in the specified format";

        Required = false;

        // TODO: Not sure if this is better
       AcceptOnlyFromAmong("json");
        // HelpName = "json";

        this.DefaultFromEnvironmentValue("OUTPUT_FORMAT");
    }
}
