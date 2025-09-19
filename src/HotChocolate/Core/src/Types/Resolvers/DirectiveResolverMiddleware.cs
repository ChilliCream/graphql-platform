namespace HotChocolate.Resolvers;

internal sealed class DirectiveDelegateMiddleware : IDirectiveMiddleware
{
    public DirectiveDelegateMiddleware(
        string directiveName,
        DirectiveMiddleware middleware)
    {
        ArgumentException.ThrowIfNullOrEmpty(directiveName);
        ArgumentNullException.ThrowIfNull(middleware);

        DirectiveName = directiveName;
        Middleware = middleware;
    }

    public string DirectiveName { get; }

    public DirectiveMiddleware Middleware { get; }
}
