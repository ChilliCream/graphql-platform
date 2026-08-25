namespace ChilliCream.Nitro.CommandLine.Commands.Tasks.Options;

internal sealed class CommentTextArgument : Argument<string>
{
    public CommentTextArgument() : base("text")
    {
        Description = "The comment text";
    }
}
