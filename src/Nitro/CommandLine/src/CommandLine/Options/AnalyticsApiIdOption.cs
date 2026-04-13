namespace ChilliCream.Nitro.CommandLine;

/// <summary>
/// The optional <c>--api-id</c> option used by analytical commands. Unlike
/// <see cref="OptionalApiIdOption"/> this option is genuinely optional because
/// analytical commands fall back to the session default persisted via
/// <c>nitro config set api</c>.
/// </summary>
internal sealed class AnalyticsApiIdOption : Option<string>
{
    public const string OptionName = ApiIdOption.OptionName;

    public AnalyticsApiIdOption() : base(OptionName)
    {
        Description = "The ID of the API";
        Required = false;
        this.DefaultFromEnvironmentValue(EnvironmentVariables.ApiId);
        this.NonEmptyStringsOnly();
    }
}
