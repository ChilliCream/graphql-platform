using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Stitching.Schemas.Products
{
    public static class ProductsSchemaRequestExecutorBuilderExtensions
    {
        public static IRequestExecutorBuilder AddProductsSchema(
            this IRequestExecutorBuilder builder)
        {
            builder.Services
                .AddSingleton<ProductRepository>();

            return builder
                .AddQueryType<Query>()
                .PublishSchemaDefinition(c => c
                    .SetName("products")
                    .IgnoreRootTypes()
                    .AddTypeExtensionsFromString(
                        @"extend type Query {
                            topProducts(first: Int = 5): [Product] @delegate
                        }

                        extend type Review {
                            product: Product @delegate(path: ""product(upc: $fields:upc)"")
                        }"));
        }
    }
}
