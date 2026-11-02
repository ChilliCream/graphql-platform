namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Tasks.Options;

internal sealed class TaskIdsArgument : Argument<string[]>
{
    public TaskIdsArgument() : base("ids")
    {
        Description = "One or more task IDs";
        Arity = ArgumentArity.OneOrMore;
    }
}
