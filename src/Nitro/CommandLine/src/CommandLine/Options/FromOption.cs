namespace ChilliCream.Nitro.CommandLine;

/// <summary>
/// The global <c>--from</c> option used by analytical commands. When unspecified the
/// command falls back to seven days before <c>--to</c>.
/// </summary>
internal sealed class FromOption : Option<DateTimeOffset?>
{
    public const string OptionName = "--from";

    public FromOption() : base(OptionName)
    {
        Description =
            "The lower bound of the time window for analytical commands "
            + "as an ISO-8601 date or date-time. "
            + "Defaults to seven days before --to.";
        Required = false;

        this.DefaultFromEnvironmentValue<DateTimeOffset?>(EnvironmentVariables.From);
    }
}
