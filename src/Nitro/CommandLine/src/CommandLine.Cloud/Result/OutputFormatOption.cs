using ChilliCream.Nitro.CLI.Option;

namespace ChilliCream.Nitro.CLI.Results;

internal sealed class OutputFormatOption : Option<OutputFormat?>
{
    public OutputFormatOption() : base("--output")
    {
        Description =
            "The format in which the result should be displayed, if this option is set, the console will be non-interactive and the result will be displayed in the specified format";

        IsRequired = false;

        this.FromAmong("json");

        this.DefaultFromEnvironmentValue("OUTPUT_FORMAT");
    }
}
