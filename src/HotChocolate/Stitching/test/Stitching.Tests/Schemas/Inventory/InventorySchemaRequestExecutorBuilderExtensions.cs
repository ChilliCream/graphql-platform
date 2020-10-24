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
                .AddQueryType<Query>()
                .PublishSchemaDefinition(c => c
                    .SetName("inventory")
                    .IgnoreRootTypes()
                    .AddTypeExtensionsFromString(
                        @"extend type Product {
                            inStock: Boolean
                                @delegate(path: ""inventoryInfo(upc: $fields:upc).isInStock"")
                            shippingEstimate: Int
                                @delegate(path: ""shippingEstimate(price: $fields:price weight: $fields:weight)"")
                        }
                "));
        }
    }
}
