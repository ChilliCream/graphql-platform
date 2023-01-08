using System;
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

        var type = new AuthorizeDirectiveType();

        return builder
            .AddDirectiveType(type)
            .TryAddSchemaDirective(type)
            .TryAddTypeInterceptor<AuthorizationTypeInterceptor>();
    }
}
