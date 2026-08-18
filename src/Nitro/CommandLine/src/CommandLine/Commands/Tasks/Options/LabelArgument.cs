namespace ChilliCream.Nitro.CommandLine.Commands.Tasks.Options;

internal sealed class LabelArgument : Argument<string>
{
    public LabelArgument() : base("label")
    {
        Description = "The label";
    }
}
