using HotChocolate;
using HotChocolate.Authorization;
using HotChocolate.Authorization.Pipeline;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides extension methods for the GraphQL builder.
/// </summary>
public static class AuthorizeRequestExecutorBuilder
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
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.TryAddSingleton<IRequestContextEnricher, AuthorizationContextEnricher>();
        builder.Services.TryAddSingleton(new AuthorizationCache());
        builder.ConfigureSchema(sb => sb.AddAuthorizeDirectiveType());
        builder.AddValidationRule(
            (s, _) => new AuthorizeValidationRule(
                s.GetRequiredService<AuthorizationCache>()));

        var prepareAuthorization = PrepareAuthorizationMiddleware.Create();
        builder.InsertUseRequest(
            before: "DocumentValidationMiddleware",
            middleware: prepareAuthorization.Middleware,
            key: prepareAuthorization.Key);

        var authorizeRequest = AuthorizeRequestMiddleware.Create();
        builder.AppendUseRequest(
            after: "DocumentValidationMiddleware",
            middleware: authorizeRequest.Middleware,
            key: authorizeRequest.Key);
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
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        builder.ConfigureSchema(sb => configure(sb.GetAuthorizationOptions()));
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
        ArgumentNullException.ThrowIfNull(builder);

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
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(factory);

        builder.AddAuthorizationCore();
        builder.Services.RemoveAll<IAuthorizationHandler>();
        builder.Services.AddScoped<IAuthorizationHandler>(factory);
        return builder;
    }
}
