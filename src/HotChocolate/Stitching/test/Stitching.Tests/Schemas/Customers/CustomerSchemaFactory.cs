using System;
using HotChocolate.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Stitching.Schemas.Customers
{
    public static class CustomerSchemaFactory
    {
        public static ISchema Create()
        {
            return SchemaBuilder.New()
                .AddQueryType<QueryType>()
                .AddMutationType<MutationType>()
                .EnableRelaySupport()
                .Create();
        }

/*
        public static void ConfigureServices(IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddSingleton<CustomerRepository>();
            services.AddSingleton<Query>();
            services.AddGraphQL(Create());
        }*/
    }
}
