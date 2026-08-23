namespace ChilliCream.Nitro.CommandLine.Commands.Tasks.Options;

internal sealed class LabelsArgument : Argument<string[]>
{
    public LabelsArgument() : base("labels")
    {
        Description = "One or more labels";
        Arity = ArgumentArity.OneOrMore;
    }
}
