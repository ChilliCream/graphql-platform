using HotChocolate;
using HotChocolate.Execution.Configuration;
using HotChocolate.Resolvers;

[assembly: Module("IntegrationTestTypesCore")]

namespace Microsoft.Extensions.DependencyInjection;

public static class IntegrationTestTypesRequestExecutorBuilderExtensions
{
    public static IRequestExecutorBuilder AddIntegrationTestTypes(
        this IRequestExecutorBuilder builder)
        => builder
            .AddIntegrationTestTypesCore()
            .AddParameterExpressionBuilder(
                static (IResolverContext context) =>
                    context.GetGlobalState<HotChocolate.Types.BatchCurrentUser>("batchCurrentUser"));
}
