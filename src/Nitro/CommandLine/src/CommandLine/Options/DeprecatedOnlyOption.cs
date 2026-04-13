namespace ChilliCream.Nitro.CommandLine;

/// <summary>
/// The optional <c>--deprecated-only</c> flag used by <c>nitro schema unused</c>. Narrows
/// the result to coordinates that are already deprecated and ready to be removed.
/// </summary>
internal sealed class DeprecatedOnlyOption : Option<bool>
{
    public const string OptionName = "--deprecated-only";

    public DeprecatedOnlyOption() : base(OptionName)
    {
        Description =
            "Only return coordinates that are marked deprecated. "
            + "Combined with the zero-usage filter this produces a 'safe to remove now' list.";
        Required = false;

        this.DefaultFromEnvironmentValue<bool>(EnvironmentVariables.DeprecatedOnly);
    }
}
