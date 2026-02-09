namespace HotChocolate.Fetching;

internal static class DataLoaderServiceProviderExtensions
{
    public static void InitializeDataLoaderScope(this IServiceProvider services)
    {
        var batchDispatcher = services.GetRequiredService<IBatchDispatcher>();
        var dataLoaderScopeHolder = services.GetRequiredService<IDataLoaderScopeFactory>();
        dataLoaderScopeHolder.BeginScope(batchDispatcher);
    }
}
