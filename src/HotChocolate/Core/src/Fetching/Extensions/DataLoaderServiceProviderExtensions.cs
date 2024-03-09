using System;
using GreenDonut.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fetching;

internal static class DataLoaderServiceProviderExtensions
{
    public static IDataLoaderContext CreateNewDataLoaderContext(this IServiceProvider services)
    {
        var factory = services.GetRequiredService<IDataLoaderContextHelper>();
        return factory.CreateNewContext();
    }
    
    public static IDataLoaderContext EnsureDataLoaderContextExists(this IServiceProvider services)
    {
        var factory = services.GetRequiredService<IDataLoaderContextHelper>();
        return factory.EnsureContextExists();
    }
}