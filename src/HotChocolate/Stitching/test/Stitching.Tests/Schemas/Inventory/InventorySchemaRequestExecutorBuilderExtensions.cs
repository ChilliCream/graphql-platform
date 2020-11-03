using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Stitching.Schemas.Inventory
{
    public static class InventorySchemaRequestExecutorBuilderExtensions
    {
        public static IRequestExecutorBuilder AddInventorySchema(
            this IRequestExecutorBuilder builder)
        {
            builder.Services
                .AddSingleton<InventoryInfoRepository>();

            return builder
                .AddQueryType<Query>();
        }
    }
}
