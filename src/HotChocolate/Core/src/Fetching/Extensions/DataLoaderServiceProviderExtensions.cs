using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fetching;

internal static class DataLoaderServiceProviderExtensions
{
    public static void InitializeDataLoaderScope(this IServiceProvider services)
    {
        var batchHandler = services.GetRequiredService<IBatchHandler>();
        var dataLoaderScopeHolder = services.GetRequiredService<IDataLoaderScopeFactory>();
        dataLoaderScopeHolder.BeginScope(batchHandler);
    }
}
