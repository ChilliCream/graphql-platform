using HotChocolate.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Stitching.Schemas.Contracts
{
    public static class ContractSchemaFactory
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
            configuration.RegisterType<LifeInsuranceContractType>();
            configuration.RegisterType<SomeOtherContractType>();
            configuration.RegisterDirective<CustomDirectiveType>();

            configuration.UseGlobalObjectIdentifier();
        }

        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<ContractStorage>();
            services.AddSingleton<Query>();
        }
    }
}
