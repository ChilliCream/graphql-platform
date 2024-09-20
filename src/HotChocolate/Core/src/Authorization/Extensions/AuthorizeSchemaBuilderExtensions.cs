using HotChocolate.Authorization;

namespace HotChocolate;

internal static class AuthorizeSchemaBuilderExtensions
{
    public static ISchemaBuilder AddAuthorizeDirectiveType(this ISchemaBuilder builder)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        var authorize = new AuthorizeDirectiveType();
        var allowAnonymous = new AllowAnonymousDirectiveType();

        return builder
            .AddDirectiveType(authorize)
            .AddDirectiveType(allowAnonymous)
            .TryAddSchemaDirective(authorize)
            .TryAddSchemaDirective(allowAnonymous)
            .TryAddTypeInterceptor<AuthorizationTypeInterceptor>();
    }
}
