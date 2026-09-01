namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Tasks.Options;

internal sealed class LabelsArgument : Argument<string[]>
{
    public LabelsArgument() : base("labels")
    {
        Description = "One or more labels";
        Arity = ArgumentArity.OneOrMore;
    }
}
