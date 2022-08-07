using System;
using HotChocolate;
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
    /// Adds the authorization support to the schema.
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
        builder.ConfigureSchema(sb => sb.AddAuthorizeDirectiveType());
        builder.Services.TryAddSingleton<IAuthorizationHandler, DefaultAuthorizationHandler>();
        return builder;
    }

    /// <summary>
    /// Adds the authorization support to the schema.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/> for chaining in more configurations.
    /// </returns>
    [Obsolete("Use AddAuthorization()")]
    public static IRequestExecutorBuilder AddAuthorizeDirectiveType(
        this IRequestExecutorBuilder builder)
        => AddAuthorization(builder);

    /// <summary>
    /// Adds a custom authorization handler.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <typeparam name="T">
    /// The custom authorization handler.
    /// </typeparam>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/> for chaining in more configurations.
    /// </returns>
    public static IRequestExecutorBuilder AddAuthorizationHandler<T>(
        this IRequestExecutorBuilder builder)
        where T : class, IAuthorizationHandler
    {
        builder.AddAuthorization();
        builder.Services.RemoveAll<IAuthorizationHandler>();
        builder.Services.AddSingleton<IAuthorizationHandler, T>();
        return builder;
    }
}
