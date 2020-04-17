using HotChocolate.Execution.Utilities;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class HotChocolateExecutionServiceCollectionExtensions
    {
        internal static IServiceCollection AddVariableCoercion(
            this IServiceCollection services) =>
            services.AddSingleton<VariableCoercionHelper>();
    }
}
