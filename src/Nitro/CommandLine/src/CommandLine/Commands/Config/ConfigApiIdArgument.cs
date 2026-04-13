namespace ChilliCream.Nitro.CommandLine.Commands.Config;

/// <summary>
/// The positional API id argument accepted by <c>nitro config set api</c>.
/// </summary>
internal sealed class ConfigApiIdArgument : Argument<string>
{
    public const string ArgumentName = "API_ID";

    public ConfigApiIdArgument() : base(ArgumentName)
    {
        Description = "The id of the API to persist as the default.";
    }
}
