namespace ChilliCream.Nitro.CommandLine;

/// <summary>
/// The global <c>--replay</c> option used by analytical commands. Reads the GraphQL
/// response from a file recorded with <see cref="RecordOption"/> instead of contacting the
/// network — used as a safety net for offline demo rehearsals.
/// </summary>
internal sealed class ReplayOption : Option<string>
{
    public const string OptionName = "--replay";

    public ReplayOption() : base(OptionName)
    {
        Description =
            "Read the GraphQL response of an analytical command from a recorded JSON file "
            + "instead of contacting the network.";
        Required = false;

        this.DefaultFromEnvironmentValue(EnvironmentVariables.Replay);
    }
}
