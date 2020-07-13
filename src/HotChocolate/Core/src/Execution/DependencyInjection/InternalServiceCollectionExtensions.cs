using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.ObjectPool;
using GreenDonut;
using HotChocolate.Execution;
using HotChocolate.Execution.Caching;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Utilities;
using HotChocolate.Fetching;
using HotChocolate.Language;
using HotChocolate.Utilities;
using System.Linq;

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
            int maximumRetained = 512)
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
            services.TryAddSingleton<ObjectPool<ResolverTask>>(
                sp => new BufferedObjectPool<ResolverTask>(t => t.Reset()));
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
            services.TryAddSingleton<IDiagnosticEvents>(sp =>
            {
                IDiagnosticEventListener[] listeners =
                    sp.GetServices<IDiagnosticEventListener>().ToArray();
                return listeners.Length switch
                {
                    0 => new NoopDiagnosticEvents(),
                    1 => listeners[0],
                    _ => new AggregateDiagnosticEvents(listeners)
                };
            });
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

        internal static IServiceCollection TryAddDefaultBatchDispatcher(
            this IServiceCollection services)
        {
            services.TryAddScoped<BatchScheduler>();
            services.TryAddScoped<IBatchScheduler>(sp => sp.GetRequiredService<BatchScheduler>());
            services.TryAddScoped<IBatchDispatcher>(sp => sp.GetRequiredService<BatchScheduler>());
            return services;
        }
    }
}
