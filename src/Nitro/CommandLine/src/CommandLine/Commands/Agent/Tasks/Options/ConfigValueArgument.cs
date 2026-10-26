namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Tasks.Options;

internal sealed class ConfigValueArgument : Argument<string>
{
    public ConfigValueArgument() : base("value")
    {
        Description = "The configuration value";
    }
}
