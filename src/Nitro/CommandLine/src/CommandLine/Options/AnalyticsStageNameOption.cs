namespace ChilliCream.Nitro.CommandLine;

/// <summary>
/// The optional <c>--stage</c> option used by analytical commands. Unlike
/// <see cref="OptionalStageNameOption"/>, which erroneously forces the option to be
/// required, this option is genuinely optional because analytical commands fall back to
/// the session default persisted via <c>nitro config set stage</c>.
/// </summary>
internal sealed class AnalyticsStageNameOption : Option<string>
{
    public const string OptionName = "--stage";

    public AnalyticsStageNameOption() : base(OptionName)
    {
        Description = "The name of the stage";
        Required = false;
        this.DefaultFromEnvironmentValue(EnvironmentVariables.Stage);
        this.NonEmptyStringsOnly();
    }
}
