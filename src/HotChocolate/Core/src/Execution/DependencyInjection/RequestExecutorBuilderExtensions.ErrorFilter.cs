using HotChocolate;
using HotChocolate.Execution;
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
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(errorFilter);

        return builder.ConfigureSchemaServices(
            s => s.AddSingleton<IErrorFilter>(
                new FuncErrorFilterWrapper(errorFilter)));
    }

    public static IRequestExecutorBuilder AddErrorFilter<T>(
        this IRequestExecutorBuilder builder,
        Func<IServiceProvider, T> factory)
        where T : class, IErrorFilter
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(factory);

        return builder.ConfigureSchemaServices(
            s => s.AddSingleton<IErrorFilter, T>(
                sp => factory(sp.GetCombinedServices())));
    }

    public static IRequestExecutorBuilder AddErrorFilter<T>(
        this IRequestExecutorBuilder builder)
        where T : class, IErrorFilter
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.TryAddSingleton<T>();
        return builder.ConfigureSchemaServices(
            s => s.AddSingleton<IErrorFilter, T>(
                sp => sp.GetRootServiceProvider().GetRequiredService<T>()));
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
