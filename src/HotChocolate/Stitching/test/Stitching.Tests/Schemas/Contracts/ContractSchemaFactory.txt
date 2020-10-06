using HotChocolate.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Stitching.Schemas.Contracts
{
    public static class ContractSchemaFactory
    {
        public static ISchema Create()
        {
            return SchemaBuilder.New()
                .AddQueryType<QueryType>()
                .AddType<LifeInsuranceContractType>()
                .AddType<SomeOtherContractType>()
                .AddDirectiveType<CustomDirectiveType>()
                .UseGlobalObjectIdentifier()
                .Create();
        }

        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<ContractStorage>();
            services.AddSingleton<Query>();
            services.AddGraphQL(Create());
        }
    }
}
