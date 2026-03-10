using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Execution.Errors;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static partial class RequestExecutorBuilderExtensions
{
    /// <summary>
    /// Adds an error filter delegate to the GraphQL configuration.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="errorFilter">
    /// A delegate that is called for each error and can modify or replace it.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/> so that configuration can be chained.
    /// </returns>
    public static IRequestExecutorBuilder AddErrorFilter(
        this IRequestExecutorBuilder builder,
        Func<IError, IError> errorFilter)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(errorFilter);

        return builder.ConfigureSchemaServices(
            s => s.AddSingleton<IErrorFilter>(
                new FuncErrorFilterWrapper(errorFilter)));
    }

    /// <summary>
    /// Adds an error filter to the GraphQL configuration.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="factory">
    /// A factory that creates the error filter instance.
    /// </param>
    /// <typeparam name="T">
    /// The <see cref="IErrorFilter"/> implementation.
    /// </typeparam>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/> so that configuration can be chained.
    /// </returns>
    /// <remarks>
    /// The <see cref="IServiceProvider"/> passed to the <paramref name="factory"/>
    /// is for the schema services. If you need to access application services
    /// you need to either make the services available in the schema services
    /// via <see cref="AddApplicationService"/> or use
    /// <see cref="ExecutionServiceProviderExtensions.GetRootServiceProvider(IServiceProvider)"/>
    /// to access the application services from within the schema service provider.
    /// </remarks>
    public static IRequestExecutorBuilder AddErrorFilter<T>(
        this IRequestExecutorBuilder builder,
        Func<IServiceProvider, T> factory)
        where T : class, IErrorFilter
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(factory);

        return builder.ConfigureSchemaServices(
            s => s.AddSingleton<IErrorFilter, T>(factory));
    }

    /// <summary>
    /// Adds an error filter to the GraphQL configuration.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <typeparam name="T">
    /// The <see cref="IErrorFilter"/> implementation.
    /// </typeparam>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/> so that configuration can be chained.
    /// </returns>
    /// <remarks>
    /// The <typeparamref name="T"/> will be activated with the <see cref="IServiceProvider"/> of the schema services.
    /// If your <typeparamref name="T"/> needs to access application services you need to
    /// make the services available in the schema services via <see cref="AddApplicationService"/>.
    /// </remarks>
    public static IRequestExecutorBuilder AddErrorFilter<T>(
        this IRequestExecutorBuilder builder)
        where T : class, IErrorFilter
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.TryAddSingleton<T>();
        return builder.ConfigureSchemaServices(
            s => s.AddSingleton<IErrorFilter, T>());
    }

    public static IServiceCollection AddErrorFilter(
        this IServiceCollection services,
        Func<IError, IError> errorFilter)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(errorFilter);

        return services.AddSingleton<IErrorFilter>(
            new FuncErrorFilterWrapper(errorFilter));
    }

    public static IServiceCollection AddErrorFilter(
        this IServiceCollection services,
        Func<IServiceProvider, IErrorFilter> factory)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(factory);

        return services.AddSingleton(factory);
    }

    public static IServiceCollection AddErrorFilter<T>(
        this IServiceCollection services)
        where T : class, IErrorFilter
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddSingleton<IErrorFilter, T>();
    }
}
