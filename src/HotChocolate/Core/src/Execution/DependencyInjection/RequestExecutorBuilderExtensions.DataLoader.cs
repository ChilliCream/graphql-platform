using System;
using GreenDonut;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static partial class RequestExecutorBuilderExtensions
{
    public static IRequestExecutorBuilder AddDataLoader<T>(
        this IRequestExecutorBuilder builder)
        where T : class, IDataLoader
    {
        builder.Services.TryAddScoped<T>();
        return builder;
    }

    public static IRequestExecutorBuilder AddDataLoader<TService, TImplementation>(
        this IRequestExecutorBuilder builder)
        where TService : class, IDataLoader
        where TImplementation : class, TService
    {
        builder.Services.TryAddScoped<TService, TImplementation>();
        return builder;
    }

    public static IRequestExecutorBuilder AddDataLoader<T>(
        this IRequestExecutorBuilder builder,
        Func<IServiceProvider, T> factory)
        where T : class, IDataLoader
    {
        builder.Services.TryAddScoped(factory);
        return builder;
    }
}
