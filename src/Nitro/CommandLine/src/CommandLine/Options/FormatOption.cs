using ChilliCream.Nitro.CommandLine.Output;

namespace ChilliCream.Nitro.CommandLine;

/// <summary>
/// The global <c>--format</c> option used by analytical commands. When unspecified the
/// command auto-detects between <see cref="OutputFormat.Table"/> (TTY) and
/// <see cref="OutputFormat.Json"/> (piped) — matching <c>gh</c> and <c>kubectl</c>.
/// </summary>
internal sealed class FormatOption : Option<OutputFormat?>
{
    public const string OptionName = "--format";

    public FormatOption() : base(OptionName)
    {
        Description =
            "The output format used by analytical commands "
            + "(table for humans, json for scripting, markdown for coding agents). "
            + "Defaults to table when stdout is a terminal and json otherwise.";
        Required = false;

        AcceptOnlyFromAmong("table", "json", "markdown");

        this.DefaultFromEnvironmentValue<OutputFormat?>(EnvironmentVariables.Format);
    }
}
