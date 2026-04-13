namespace ChilliCream.Nitro.CommandLine;

/// <summary>
/// The global <c>--to</c> option used by analytical commands. When unspecified the command
/// falls back to the current UTC time.
/// </summary>
internal sealed class ToOption : Option<DateTimeOffset?>
{
    public const string OptionName = "--to";

    public ToOption() : base(OptionName)
    {
        Description =
            "The upper bound of the time window for analytical commands "
            + "as an ISO-8601 date or date-time. "
            + "Defaults to the current time.";
        Required = false;

        this.DefaultFromEnvironmentValue<DateTimeOffset?>(EnvironmentVariables.To);
    }
}
