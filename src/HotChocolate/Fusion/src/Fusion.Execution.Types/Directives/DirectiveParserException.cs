namespace HotChocolate.Fusion.Types.Directives;

internal class DirectiveParserException : Exception
{
    public DirectiveParserException()
    {
    }

    public DirectiveParserException(string message) : base(message)
    {
    }

    public DirectiveParserException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
