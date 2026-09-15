using GreenDonut;
using HotChocolate;
using HotChocolate.Execution.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Types;

[assembly: Module("IntegrationTestTypesCore")]
[assembly: DataLoaderModule("IntegrationTestTypesCore")]

namespace Microsoft.Extensions.DependencyInjection;

public static class IntegrationTestTypesRequestExecutorBuilderExtensions
{
    public static IRequestExecutorBuilder AddIntegrationTestTypes(
        this IRequestExecutorBuilder builder)
    {
        builder.Services
            .AddSingleton(new BatchNodeAuthorizationProbe(true))
            .AddSingleton(new BatchAuthorizationProbe(true));

        return builder
            .AddAuthorizationHandler(s => s.GetRequiredService<BatchNodeAuthorizationProbe>())
            .AddIntegrationTestTypesCore()
            .AddParameterExpressionBuilder(
                static (IResolverContext context) =>
                    context.GetGlobalState<HotChocolate.Types.BatchCurrentUser>("batchCurrentUser"));
    }
}
