namespace HotChocolate.Resolvers;

internal sealed class DirectiveDelegateMiddleware : IDirectiveMiddleware
{
    public DirectiveDelegateMiddleware(
        string directiveName,
        DirectiveMiddleware middleware)
    {
        if (string.IsNullOrEmpty(directiveName))
        {
            throw new ArgumentNullException(nameof(directiveName));
        }

        if (middleware is null)
        {
            throw new ArgumentNullException(nameof(middleware));
        }

        DirectiveName = directiveName;
        Middleware = middleware;
    }

    public string DirectiveName { get; }

    public DirectiveMiddleware Middleware { get; }
}
