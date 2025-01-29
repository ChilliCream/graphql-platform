using GreenDonut;
using GreenDonut.DependencyInjection;
using HotChocolate.Execution.Configuration;
using HotChocolate.Fetching;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class RequestExecutorBuilderExtensions
{
    public static IRequestExecutorBuilder AddDataLoader<T>(
        this IRequestExecutorBuilder builder)
        where T : class, IDataLoader
    {
        builder.Services.AddSingleton(new DataLoaderRegistration(typeof(T)));
        builder.Services.TryAddScoped<T>(sp => sp.GetDataLoader<T>());
        return builder;
    }

    public static IRequestExecutorBuilder AddDataLoader<TService, TImplementation>(
        this IRequestExecutorBuilder builder)
        where TService : class, IDataLoader
        where TImplementation : class, TService
    {
        builder.Services.AddSingleton(new DataLoaderRegistration(typeof(TService), typeof(TImplementation)));
        builder.Services.TryAddScoped<TImplementation>(sp => sp.GetDataLoader<TImplementation>());
        builder.Services.TryAddScoped<TService>(sp => sp.GetDataLoader<TService>());
        return builder;
    }

    public static IRequestExecutorBuilder AddDataLoader<T>(
        this IRequestExecutorBuilder builder,
        Func<IServiceProvider, T> factory)
        where T : class, IDataLoader
    {
        builder.Services.AddSingleton(new DataLoaderRegistration(typeof(T), sp => factory(sp)));
        builder.Services.TryAddScoped<T>(sp => sp.GetDataLoader<T>());
        return builder;
    }

    public static IRequestExecutorBuilder AddDataLoader<TService, TImplementation>(
        this IRequestExecutorBuilder builder,
        Func<IServiceProvider, TImplementation> factory)
        where TService : class, IDataLoader
        where TImplementation : class, TService
    {
        builder.Services.AddSingleton(new DataLoaderRegistration(typeof(TService), typeof(TImplementation), sp => factory(sp)));
        builder.Services.TryAddScoped<TImplementation>(sp => sp.GetDataLoader<TImplementation>());
        builder.Services.TryAddScoped<TService>(sp => sp.GetDataLoader<TService>());
        return builder;
    }
}

file static class DataLoaderServiceProviderExtensions
{
    public static T GetDataLoader<T>(this IServiceProvider services) where T : IDataLoader
        => services.GetRequiredService<IDataLoaderScope>().GetDataLoader<T>();
}
