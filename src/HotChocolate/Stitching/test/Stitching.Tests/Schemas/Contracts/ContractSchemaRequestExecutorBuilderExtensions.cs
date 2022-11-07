using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Stitching.Schemas.Contracts
{
    public static class ContractSchemaRequestExecutorBuilderExtensions
    {
        public static IRequestExecutorBuilder AddContractSchema(
            this IRequestExecutorBuilder builder)
        {
            builder.Services
                .AddSingleton<ContractStorage>()
                .AddSingleton<Query>();

            return builder
                .AddQueryType<QueryType>()
                .AddType<LifeInsuranceContractType>()
                .AddType<SomeOtherContractType>()
                .AddDirectiveType<CustomDirectiveType>()
                .EnableRelaySupport();
        }
    }
}
