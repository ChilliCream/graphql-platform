using Microsoft.Extensions.DependencyInjection;
using HotChocolate.DataLoader;
using HotChocolate.Execution;

namespace HotChocolate
{
    public static class DataLoaderServiceCollectionExtensions
    {
        public static IServiceCollection AddDataLoaderRegistry(
            this IServiceCollection services)
        {
            return services
                .AddScoped<IDataLoaderRegistry, DataLoaderRegistry>()
                .AddScoped<IBatchOperation>(sp =>
                {
                    var batchOperation = new DataLoaderBatchOperation();

                    foreach (IDataLoaderRegistry registry in
                        sp.GetServices<IDataLoaderRegistry>())
                    {
                        registry.Subscribe(batchOperation);
                    }

                    return batchOperation;
                });
        }
    }
}
