namespace ChilliCream.Nitro.CommandLine.Commands.Config;

/// <summary>
/// The positional format argument accepted by <c>nitro config set format</c>. Constrained
/// to the values of <see cref="ChilliCream.Nitro.CommandLine.Output.OutputFormat"/>.
/// </summary>
internal sealed class ConfigFormatArgument : Argument<string>
{
    public const string ArgumentName = "FORMAT";

    public ConfigFormatArgument() : base(ArgumentName)
    {
        Description = "The default output format (table, json, markdown).";

        this.AcceptOnlyFromAmong("table", "json", "markdown");
    }
}
