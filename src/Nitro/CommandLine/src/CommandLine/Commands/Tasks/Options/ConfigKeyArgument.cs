namespace ChilliCream.Nitro.CommandLine.Commands.Tasks.Options;

internal sealed class ConfigKeyArgument : Argument<string>
{
    public ConfigKeyArgument() : base("key")
    {
        Description = "The configuration key";
    }
}
