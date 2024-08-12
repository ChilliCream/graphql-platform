using HotChocolate;
using HotChocolate.Authorization;
using HotChocolate.Execution;
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
    /// The GraphQL configuration builder.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/> for chaining in more configurations.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="builder"/> is <c>null</c>.
    /// </exception>
    public static IRequestExecutorBuilder AddAuthorizationCore(
        this IRequestExecutorBuilder builder)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.Services.TryAddSingleton<IRequestContextEnricher, AuthorizationContextEnricher>();
        builder.Services.TryAddSingleton(new AuthorizationCache());
        builder.ConfigureSchema(sb => sb.AddAuthorizeDirectiveType());
        builder.AddValidationRule(
            (s, _) => new AuthorizeValidationRule(
                s.GetRequiredService<AuthorizationCache>()));
        builder.AddValidationResultAggregator(
            (s, _) => new AuthorizeValidationResultAggregator(s));
        return builder;
    }

    /// <summary>
    /// Modify authorization options.
    /// </summary>
    /// <param name="builder">
    /// The GraphQL configuration builder.
    /// </param>
    /// <param name="configure">
    /// A delegate to mutate the configuration object.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/> for chaining in more configurations.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="builder"/> or <paramref name="configure"/> is <c>null</c>.
    /// </exception>
    public static IRequestExecutorBuilder ModifyAuthorizationOptions(
        this IRequestExecutorBuilder builder,
        Action<AuthorizationOptions> configure)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

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
    /// The GraphQL configuration builder.
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
        builder.Services.AddScoped<IAuthorizationHandler, T>();
        return builder;
    }

    /// <summary>
    /// Adds a custom authorization handler.
    /// </summary>
    /// <param name="builder">
    /// The GraphQL configuration builder.
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
        builder.Services.AddScoped<IAuthorizationHandler>(factory);
        return builder;
    }
}
