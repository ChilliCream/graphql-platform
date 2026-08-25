namespace ChilliCream.Nitro.CommandLine.Commands.Tasks.Options;

internal sealed class ConfigValueArgument : Argument<string>
{
    public ConfigValueArgument() : base("value")
    {
        Description = "The configuration value";
    }
}
