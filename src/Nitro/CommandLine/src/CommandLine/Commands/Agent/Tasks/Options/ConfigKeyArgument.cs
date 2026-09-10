namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Tasks.Options;

internal sealed class ConfigKeyArgument : Argument<string>
{
    public ConfigKeyArgument() : base("key")
    {
        Description = "The configuration key";
    }
}
