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
                .AddQueryType<Query>();
        }
    }
}
