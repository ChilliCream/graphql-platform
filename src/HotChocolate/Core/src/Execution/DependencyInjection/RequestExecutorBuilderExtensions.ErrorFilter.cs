using HotChocolate;
using HotChocolate.Execution.Configuration;
using HotChocolate.Execution.Errors;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static partial class RequestExecutorBuilderExtensions
{
    public static IRequestExecutorBuilder AddErrorFilter(
        this IRequestExecutorBuilder builder,
        Func<IError, IError> errorFilter)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (errorFilter is null)
        {
            throw new ArgumentNullException(nameof(errorFilter));
        }

        return builder.ConfigureSchemaServices(
            s => s.AddSingleton<IErrorFilter>(
                new FuncErrorFilterWrapper(errorFilter)));
    }

    public static IRequestExecutorBuilder AddErrorFilter<T>(
        this IRequestExecutorBuilder builder,
        Func<IServiceProvider, T> factory)
        where T : class, IErrorFilter
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (factory is null)
        {
            throw new ArgumentNullException(nameof(factory));
        }

        return builder.ConfigureSchemaServices(
            s => s.AddSingleton<IErrorFilter, T>(
                sp => factory(sp.GetCombinedServices())));
    }

    public static IRequestExecutorBuilder AddErrorFilter<T>(
        this IRequestExecutorBuilder builder)
        where T : class, IErrorFilter
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.Services.TryAddSingleton<T>();
        return builder.ConfigureSchemaServices(
            s => s.AddSingleton<IErrorFilter, T>(
                sp => sp.GetApplicationService<T>()));
    }

    public static IServiceCollection AddErrorFilter(
        this IServiceCollection services,
        Func<IError, IError> errorFilter)
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (errorFilter is null)
        {
            throw new ArgumentNullException(nameof(errorFilter));
        }

        return services.AddSingleton<IErrorFilter>(
            new FuncErrorFilterWrapper(errorFilter));
    }

    public static IServiceCollection AddErrorFilter(
        this IServiceCollection services,
        Func<IServiceProvider, IErrorFilter> factory)
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (factory is null)
        {
            throw new ArgumentNullException(nameof(factory));
        }

        return services.AddSingleton(factory);
    }

    public static IServiceCollection AddErrorFilter<T>(
        this IServiceCollection services)
        where T : class, IErrorFilter
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        return services.AddSingleton<IErrorFilter, T>();
    }
}
