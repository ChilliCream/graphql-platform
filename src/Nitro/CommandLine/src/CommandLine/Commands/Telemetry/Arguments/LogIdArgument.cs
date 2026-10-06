namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Arguments;

internal sealed class LogIdArgument : Argument<string>
{
    public LogIdArgument() : base("log-id")
    {
        Description = "The log ID";
        Arity = ArgumentArity.ExactlyOne;
    }
}
