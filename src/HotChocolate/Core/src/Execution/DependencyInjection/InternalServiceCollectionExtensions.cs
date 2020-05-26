using HotChocolate.Execution;
using HotChocolate.Execution.Caching;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Utilities;
using HotChocolate.Language;
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

        internal static IServiceCollection TryAddOperationContextPool(
            this IServiceCollection services,
            int maximumRetained = 16)
        {
            services.TryAddTransient<OperationContext>();
            services.TryAddSingleton<ObjectPool<OperationContext>>(
                sp => new OperationContextPool(() => sp.GetRequiredService<OperationContext>(),
                maximumRetained));
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

        internal static IServiceCollection TryAddOperationExecutors(
            this IServiceCollection services)
        {
            services.TryAddSingleton<QueryExecutor>();
            services.TryAddSingleton<MutationExecutor>();
            services.TryAddSingleton<SubscriptionExecutor>();
            return services;
        }

        internal static IServiceCollection TryAddNoOpDiagnostics(
            this IServiceCollection services)
        {
            services.TryAddSingleton<IDiagnosticEvents, NoopDiagnosticEvents>();
            return services;
        }

        internal static IServiceCollection TryAddDefaultCaches(
            this IServiceCollection services)
        {
            services.TryAddSingleton<IDocumentCache>(
                sp => new DefaultDocumentCache());
            services.TryAddSingleton<IPreparedOperationCache>(
                sp => new DefaultPreparedOperationCache());
            return services;
        }

        internal static IServiceCollection TryAddDefaultDocumentHashProvider(
            this IServiceCollection services)
        {
            services.TryAddSingleton<IDocumentHashProvider>(
                sp => new MD5DocumentHashProvider());
            return services;
        }
    }
}
