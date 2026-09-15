using HotChocolate.Authorization;
using HotChocolate.Resolvers;

namespace HotChocolate.Types.BatchResolvers;

public sealed class TestAuthHandler : IAuthorizationHandler
{
    public Func<IMiddlewareContext, AuthorizeDirective, AuthorizeResult> Resolver { get; set; }
        = static (_, _) => AuthorizeResult.Allowed;

    public Func<AuthorizationContext, AuthorizeDirective, AuthorizeResult> Validation { get; set; }
        = static (_, _) => AuthorizeResult.Allowed;

    public ValueTask<AuthorizeResult> AuthorizeAsync(
        IMiddlewareContext context,
        AuthorizeDirective directive,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return new(Resolver(context, directive));
    }

    public ValueTask<AuthorizeResult> AuthorizeAsync(
        AuthorizationContext context,
        IReadOnlyList<AuthorizeDirective> directives,
        CancellationToken cancellationToken)
    {
        foreach (var directive in directives)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = Validation(context, directive);

            if (result is not AuthorizeResult.Allowed)
            {
                return new(result);
            }
        }

        return new(AuthorizeResult.Allowed);
    }
}
