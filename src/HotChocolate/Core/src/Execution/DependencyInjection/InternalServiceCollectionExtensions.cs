using System;
using System.Linq;
using GreenDonut;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Execution.Caching;
using HotChocolate.Execution.Configuration;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Internal;
using HotChocolate.Execution.Processing;
using HotChocolate.Execution.Processing.Tasks;
using HotChocolate.Fetching;
using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Relay;
using HotChocolate.Utilities;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

internal static class InternalServiceCollectionExtensions
{
    internal static IServiceCollection TryAddRequestExecutorFactoryOptionsMonitor(
        this IServiceCollection services)
    {
        services.TryAddSingleton<IRequestExecutorOptionsMonitor>(
            sp => new DefaultRequestExecutorOptionsMonitor(
                sp.GetRequiredService<IOptionsMonitor<RequestExecutorSetup>>(),
                sp.GetServices<IRequestExecutorOptionsProvider>()));
        return services;
    }

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
            _ => new ResultMapPool(maximumRetained));
        services.TryAddSingleton<ObjectPool<ResultObjectBuffer<ResultMapList>>>(
            _ => new ResultMapListPool(maximumRetained));
        services.TryAddSingleton<ObjectPool<ResultObjectBuffer<ResultList>>>(
            _ => new ResultListPool(maximumRetained));
        services.TryAddSingleton<ResultPool>();
        return services;
    }

    internal static IServiceCollection TryAddResolverTaskPool(
        this IServiceCollection services,
        int maximumRetained = 128)
    {
        services.TryAddSingleton<ObjectPool<ResolverTask>>(
            _ => new ExecutionTaskPool<ResolverTask>(
                new ResolverTaskPoolPolicy(),
                maximumRetained));

        return services;
    }

    internal static IServiceCollection TryAddPathSegmentPool(
        this IServiceCollection services,
        int maximumRetained = 256)
    {
        services.TryAddSingleton<ObjectPool<PathSegmentBuffer<IndexerPathSegment>>>(
            _ => new IndexerPathSegmentPool(maximumRetained));
        services.TryAddSingleton<ObjectPool<PathSegmentBuffer<NamePathSegment>>>(
            _ => new NamePathSegmentPool(maximumRetained));
        return services;
    }

    internal static IServiceCollection TryAddOperationContextPool(
        this IServiceCollection services)
    {
        services.TryAddSingleton(sp =>
        {
            ObjectPoolProvider provider = sp.GetRequiredService<ObjectPoolProvider>();
            var policy = new OperationContextPooledObjectPolicy(
                sp.GetRequiredService<ObjectPool<ResolverTask>>(),
                sp.GetRequiredService<ResultPool>(),
                sp.GetRequiredService<ObjectPool<PathSegmentBuffer<IndexerPathSegment>>>(),
                sp.GetRequiredService<ObjectPool<PathSegmentBuffer<NamePathSegment>>>());
            return provider.Create(policy);
        });

        return services;
    }

    internal static IServiceCollection TryAddDataLoaderTaskCachePool(
        this IServiceCollection services)
    {
        services.TryAddSingleton(
            sp => TaskCachePool.Create(sp.GetRequiredService<ObjectPoolProvider>()));
        services.TryAddScoped(
            sp => new TaskCacheOwner(sp.GetRequiredService<ObjectPool<TaskCache>>()));
        return services;
    }

    internal static IServiceCollection TryAddDataLoaderOptions(
        this IServiceCollection services)
    {
        services.TryAddSingleton<IDataLoaderDiagnosticEvents>(
            sp =>
            {
                IDataLoaderDiagnosticEventListener[] listeners =
                    sp.GetServices<IDataLoaderDiagnosticEventListener>().ToArray();

                return listeners.Length switch
                {
                    0 => new DataLoaderDiagnosticEventListener(),
                    1 => listeners[0],
                    _ => new AggregateDataLoaderDiagnosticEventListener(listeners)
                };
            });

        services.TryAddScoped(
            sp => new DataLoaderOptions
            {
                Caching = true,
                Cache = sp.GetRequiredService<TaskCacheOwner>().Cache,
                DiagnosticEvents = sp.GetService<IDataLoaderDiagnosticEvents>(),
                MaxBatchSize = 1024
            });
        return services;
    }

    internal static IServiceCollection TryAddTypeConverter(
        this IServiceCollection services)
    {
        services.TryAddSingleton<ITypeConverter>(
            sp => new DefaultTypeConverter(sp.GetServices<IChangeTypeProvider>()));
        return services;
    }

    internal static IServiceCollection TryAddInputFormatter(
        this IServiceCollection services)
    {
        services.TryAddSingleton(sp => new InputFormatter(sp.GetTypeConverter()));
        return services;
    }

    internal static IServiceCollection TryAddInputParser(
        this IServiceCollection services)
    {
        services.TryAddSingleton(sp => new InputParser(sp.GetTypeConverter()));
        return services;
    }

    internal static IServiceCollection TryAddRequestExecutorResolver(
        this IServiceCollection services)
    {
        services.TryAddSingleton<RequestExecutorResolver>();
        services.TryAddSingleton<IRequestExecutorResolver>(
            sp => sp.GetRequiredService<RequestExecutorResolver>());
        services.TryAddSingleton<IInternalRequestExecutorResolver>(
            sp => sp.GetRequiredService<RequestExecutorResolver>());
        return services;
    }

    internal static IServiceCollection TryAddDefaultCaches(
        this IServiceCollection services)
    {
        services.TryAddSingleton<IDocumentCache>(
            _ => new DefaultDocumentCache());
        services.TryAddSingleton<IPreparedOperationCache>(
            _ => new DefaultPreparedOperationCache());
        services.TryAddSingleton<IComplexityAnalyzerCache>(
            _ => new DefaultComplexityAnalyzerCache());
        services.TryAddSingleton<IQueryPlanCache>(
            _ => new DefaultQueryPlanCache());
        return services;
    }

    internal static IServiceCollection TryAddDefaultDocumentHashProvider(
        this IServiceCollection services)
    {
        services.TryAddSingleton<IDocumentHashProvider>(
            _ => new MD5DocumentHashProvider());
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

    internal static IServiceCollection TryAddRequestContextAccessor(
        this IServiceCollection services)
    {
        services.TryAddSingleton<DefaultRequestContextAccessor>();
        services.TryAddSingleton<IRequestContextAccessor>(
            sp => sp.GetRequiredService<DefaultRequestContextAccessor>());
        return services;
    }

    internal static IServiceCollection TryAddDefaultDataLoaderRegistry(
        this IServiceCollection services)
    {
        services.TryAddScoped<IDataLoaderRegistry, DefaultDataLoaderRegistry>();
        return services;
    }

    internal static IServiceCollection TryAddIdSerializer(
        this IServiceCollection services)
    {
        services.TryAddSingleton<IIdSerializer, IdSerializer>();
        return services;
    }

    internal static IServiceCollection TryAddDataLoaderParameterExpressionBuilder(
        this IServiceCollection services)
        => services.TryAddParameterExpressionBuilder<DataLoaderParameterExpressionBuilder>();

    internal static IServiceCollection TryAddParameterExpressionBuilder<T>(
        this IServiceCollection services)
        where T : class, IParameterExpressionBuilder
    {
        if (services.All(t => t.ImplementationType != typeof(T)))
        {
            services.AddSingleton<IParameterExpressionBuilder, T>();
        }
        return services;
    }

    internal static IServiceCollection AddParameterExpressionBuilder<T>(
        this IServiceCollection services,
        Func<IServiceProvider, T> factory)
        where T : class, IParameterExpressionBuilder
    {
        services.AddSingleton<IParameterExpressionBuilder>(factory);
        return services;
    }

    private sealed class OperationContextPooledObjectPolicy : PooledObjectPolicy<OperationContext>
    {
        private readonly ObjectPool<ResolverTask> _resolverTaskPool;
        private readonly ResultPool _resultPool;
        private readonly ObjectPool<PathSegmentBuffer<IndexerPathSegment>> _indexerPathSegmentPool;
        private readonly ObjectPool<PathSegmentBuffer<NamePathSegment>> _namePathSegmentPool;

        public OperationContextPooledObjectPolicy(
            ObjectPool<ResolverTask> resolverTaskPool,
            ResultPool resultPool,
            ObjectPool<PathSegmentBuffer<IndexerPathSegment>> indexerPathSegmentPool,
            ObjectPool<PathSegmentBuffer<NamePathSegment>> namePathSegmentPool)
        {
            _resolverTaskPool = resolverTaskPool ??
                throw new ArgumentNullException(nameof(resolverTaskPool));
            _resultPool = resultPool ??
                throw new ArgumentNullException(nameof(resultPool));
            _indexerPathSegmentPool = indexerPathSegmentPool ??
                throw new ArgumentNullException(nameof(indexerPathSegmentPool));
            _namePathSegmentPool = namePathSegmentPool ??
                throw new ArgumentNullException(nameof(namePathSegmentPool));
        }

        public override OperationContext Create()
            => new(_resolverTaskPool, _resultPool, _indexerPathSegmentPool, _namePathSegmentPool);

        public override bool Return(OperationContext obj)
        {
            if (!obj.IsInitialized)
            {
                return true;
            }

            // if work related to the operation context has completed we can
            // reuse the operation context.
            if (obj.Scheduler.IsCompleted)
            {
                obj.Clean();
                return true;
            }

            // we also clean if we cannot reuse the context so that the context is
            // gracefully discarded and can be garbage collected.
            obj.Clean();
            return false;
        }
    }
}
