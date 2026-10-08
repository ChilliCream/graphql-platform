namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Arguments;

internal sealed class AttributeKeyArgument : Argument<string>
{
    public AttributeKeyArgument() : base("key")
    {
        Description = "The attribute key";
        Arity = ArgumentArity.ExactlyOne;
    }
}
