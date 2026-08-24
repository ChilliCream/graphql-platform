namespace ChilliCream.Nitro.CommandLine.Commands.Tasks.Options;

internal sealed class DependsOnIdArgument : Argument<string>
{
    public DependsOnIdArgument() : base("depends-on-id")
    {
        Description = "The task this dependency points to";
    }
}
