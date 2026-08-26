namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Tasks.Options;

internal sealed class OptionalTaskIdArgument : Argument<string?>
{
    public OptionalTaskIdArgument() : base("id")
    {
        Description = "The task ID";
        Arity = ArgumentArity.ZeroOrOne;
    }
}
