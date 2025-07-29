using HotChocolate.AspNetCore.Authorization;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddAuthorization();
        builder.AddAuthorizationServices();
        return builder;
    }

    /// <summary>
    /// Adds the default authorization support to the schema that
    /// uses Microsoft.AspNetCore.Authorization.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="configure">
    /// An action delegate to configure the provided
    /// <see cref="AspNetCore.Authorization.AuthorizationOptions"/>.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/> for chaining in more configurations.
    /// </returns>
    public static IRequestExecutorBuilder AddAuthorization(
        this IRequestExecutorBuilder builder,
        Action<AspNetCore.Authorization.AuthorizationOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        builder.Services.AddAuthorization(configure);
        builder.AddAuthorizationServices();
        return builder;
    }

    private static void AddAuthorizationServices(this IRequestExecutorBuilder builder)
    {
        builder.Services.TryAddSingleton<AuthorizationPolicyCache>();
        builder.AddAuthorizationHandler<DefaultAuthorizationHandler>();
    }
}
