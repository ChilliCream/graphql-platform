using HotChocolate.Authorization;

namespace HotChocolate;

/// <summary>
/// Provides extension methods for the schema builder.
/// </summary>
public static class AuthorizeSchemaBuilderExtensions
{
    /// <summary>
    /// Adds the authorize directive types to the schema.
    /// </summary>
    /// <param name="builder">
    /// The schema builder.
    /// </param>
    /// <returns>
    /// Returns the schema builder for configuration chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="builder"/> is <c>null</c>.
    /// </exception>
    public static ISchemaBuilder AddAuthorizeDirectiveType(this ISchemaBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (!builder.AreAuthorizeDirectivesRegistered())
        {
            var authorize = new AuthorizeDirectiveType();
            var allowAnonymous = new AllowAnonymousDirectiveType();

            builder
                .AddDirectiveType(authorize)
                .AddDirectiveType(allowAnonymous)
                .TryAddSchemaDirective(authorize)
                .TryAddSchemaDirective(allowAnonymous)
                .TryAddTypeInterceptor<AuthorizationTypeInterceptor>()
                .MarkAuthorizeDirectivesRegistered();
        }

        return builder;
    }
}
