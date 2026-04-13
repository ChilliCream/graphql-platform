namespace ChilliCream.Nitro.CommandLine;

/// <summary>
/// The global <c>--record</c> option used by analytical commands. Captures the raw GraphQL
/// response to a JSON file after a successful run so it can be replayed by
/// <see cref="ReplayOption"/> in offline rehearsals.
/// </summary>
internal sealed class RecordOption : Option<string>
{
    public const string OptionName = "--record";

    public RecordOption() : base(OptionName)
    {
        Description =
            "Capture the GraphQL response of an analytical command to the given file "
            + "after success. Useful for rehearsing demos with --replay.";
        Required = false;

        this.DefaultFromEnvironmentValue(EnvironmentVariables.Record);
    }
}
