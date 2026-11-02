namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry.Arguments;

internal sealed class TraceIdArgument : Argument<string>
{
    public TraceIdArgument() : base("trace-id")
    {
        Description = "The trace ID";
        Arity = ArgumentArity.ExactlyOne;
    }
}
