using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Stitching.Schemas.Customers
{
    public static class CustomerSchemaRequestExecutorBuilderExtensions
    {
        public static IRequestExecutorBuilder AddCustomerSchema(
            this IRequestExecutorBuilder builder)
        {
            builder.Services
                .AddSingleton<CustomerRepository>()
                .AddSingleton<Query>();

            return builder
                .AddQueryType<QueryType>()
                .AddMutationType<MutationType>()
                .EnableRelaySupport();
        }
    }
}
