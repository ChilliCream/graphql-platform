namespace ChilliCream.Nitro.CommandLine.Arguments;

internal sealed class FusionSettingsValueArgument : Argument<string>
{
    public const string ArgumentName = "SETTING_VALUE";

    public FusionSettingsValueArgument() : base(ArgumentName)
    {
        Description = "The value to set";
    }
}
