using System;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Stitching.Schemas.Customers
{
    public static class CustomerSchemaFactory
    {
        public static ISchema Create()
        {
            return Schema.Create(c =>
            {
                ConfigureSchema(c);
            });
        }

        public static void ConfigureSchema(ISchemaConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new System.ArgumentNullException(nameof(configuration));
            }

            configuration.RegisterQueryType<QueryType>();
            configuration.RegisterMutationType<MutationType>();
            configuration.UseGlobalObjectIdentifier();
        }

        public static void ConfigureServices(IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddSingleton<CustomerRepository>();
            services.AddSingleton<Query>();
        }
    }
}
