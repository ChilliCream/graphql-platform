namespace ChilliCream.Nitro.CommandLine.Commands.Tasks.Options;

internal sealed class SearchTextArgument : Argument<string>
{
    public SearchTextArgument() : base("text")
    {
        Description = "The search text";
    }
}
