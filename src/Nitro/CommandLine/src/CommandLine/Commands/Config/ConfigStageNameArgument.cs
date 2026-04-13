namespace ChilliCream.Nitro.CommandLine.Commands.Config;

/// <summary>
/// The positional stage name argument accepted by <c>nitro config set stage</c>.
/// </summary>
internal sealed class ConfigStageNameArgument : Argument<string>
{
    public const string ArgumentName = "STAGE";

    public ConfigStageNameArgument() : base(ArgumentName)
    {
        Description = "The name of the stage to persist as the default.";
    }
}
