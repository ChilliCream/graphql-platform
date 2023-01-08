using HotChocolate.AspNetCore.Authorization;
using HotChocolate.Execution.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides extension methods for the GraphQL builder.
/// </summary>
public static class HotChocolateAuthorizeRequestExecutorBuilder
{
    /// <summary>
    /// Adds the default authorization support to the schema that
    /// uses Microsoft.AspNetCore.Authorization.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/> for chaining in more configurations.
    /// </returns>
    public static IRequestExecutorBuilder AddAuthorization(
        this IRequestExecutorBuilder builder)
    {
        builder.AddAuthorizationHandler<DefaultAuthorizationHandler>();
        return builder;
    }
}
