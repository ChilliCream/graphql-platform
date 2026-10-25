namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Arguments;

internal sealed class ServiceNameArgument : Argument<string>
{
    public ServiceNameArgument() : base("name")
    {
        Description = "The service name";
        Arity = ArgumentArity.ExactlyOne;
    }
}
