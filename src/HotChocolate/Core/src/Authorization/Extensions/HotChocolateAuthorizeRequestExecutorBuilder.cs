using System;
using System.Security.AccessControl;
using HotChocolate;
using HotChocolate.Authorization;
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
    public static IRequestExecutorBuilder AddAuthorizationCore(
        this IRequestExecutorBuilder builder)
    {
        builder.ConfigureSchema(
            sb => sb.AddAuthorizeDirectiveType());
        builder.Services.TryAddSingleton(
            new AuthorizationCache());
        builder.AddValidationRule(
            (s, _) => new AuthorizeValidationRule(
                s.GetRequiredService<AuthorizationCache>()));
        builder.AddValidationResultAggregator(
            (s, _) => new AuthorizeValidationResultAggregator(
                s.GetRequiredService<IAuthorizationHandler>(),
                s));
        return builder;
    }

    public static IRequestExecutorBuilder ModifyAuthorizationOptions(
        this IRequestExecutorBuilder builder,
        Action<AuthorizationOptions> configure)
    {
        builder.ConfigureSchema(
            sb =>
            {
                const string key = WellKnownContextData.AuthorizationOptions;

                if (!sb.ContextData.TryGetValue(key, out var value) ||
                    value is not AuthorizationOptions options)
                {
                    options = new AuthorizationOptions();
                    sb.ContextData.Add(key, options);
                }

                configure(options);
            });
        return builder;
    }

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
        builder.AddAuthorizationCore();
        builder.Services.RemoveAll<IAuthorizationHandler>();
        builder.Services.AddSingleton<IAuthorizationHandler, T>();
        return builder;
    }

    /// <summary>
    /// Adds a custom authorization handler.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="factory">
    /// The handler factory.
    /// </param>
    /// <typeparam name="T">
    /// The custom authorization handler.
    /// </typeparam>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/> for chaining in more configurations.
    /// </returns>
    public static IRequestExecutorBuilder AddAuthorizationHandler<T>(
        this IRequestExecutorBuilder builder,
        Func<IServiceProvider, T> factory)
        where T : class, IAuthorizationHandler
    {
        builder.AddAuthorizationCore();
        builder.Services.RemoveAll<IAuthorizationHandler>();
        builder.Services.AddSingleton<IAuthorizationHandler>(factory);
        return builder;
    }
}
