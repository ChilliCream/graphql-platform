using HotChocolate.Execution;
using HotChocolate.Execution.Utilities;
using HotChocolate.Utilities;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.Extensions.DependencyInjection
{
    internal static class InternalServiceCollectionExtensions
    {
        internal static IServiceCollection TryAddVariableCoercion(
            this IServiceCollection services)
        {
            services.TryAddSingleton<VariableCoercionHelper>();
            return services;
        }

        internal static IServiceCollection TryAddResultPool(
            this IServiceCollection services,
            int maximumRetained = 16)
        {
            services.TryAddSingleton<ObjectPool<ResultObjectBuffer<ResultMap>>>(
                sp => new ResultMapPool(maximumRetained));
            services.TryAddSingleton<ObjectPool<ResultObjectBuffer<ResultMapList>>>(
                sp => new ResultMapListPool(maximumRetained));
            services.TryAddSingleton<ObjectPool<ResultObjectBuffer<ResultList>>>(
                sp => new ResultListPool(maximumRetained));
            services.TryAddSingleton<ResultPool>();
            return services;
        }

        internal static IServiceCollection TryAddResolverTaskPool(
            this IServiceCollection services,
            int maximumRetained = 16)
        {
            services.TryAddSingleton<ObjectPool<ObjectBuffer<ResolverTask>>>(
                sp => new DefaultObjectPool<ObjectBuffer<ResolverTask>>(
                    new ObjectBufferPolicy<ResolverTask>(16, task => task.Reset()),
                    maximumRetained));
            services.TryAddSingleton<ObjectPool<ResolverTask>, BufferedObjectPool<ResolverTask>>();
            return services;
        }

        internal static IServiceCollection TryAddOperationContext(
            this IServiceCollection services)
        {
            services.TryAddTransient<OperationContext>();
            return services;
        }

        internal static IServiceCollection TryAddTypeConversion(
            this IServiceCollection services)
        {
            services.TryAddSingleton<ITypeConversion, TypeConversion>();
            return services;
        }

        internal static IServiceCollection TryAddRequestExecutorResolver(
            this IServiceCollection services)
        {
            services.TryAddSingleton<IRequestExecutorResolver, RequestExecutorResolver>();
            return services;
        }
    }
}
