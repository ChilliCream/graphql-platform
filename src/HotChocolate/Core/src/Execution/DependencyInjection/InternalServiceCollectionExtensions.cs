using System;
using GreenDonut;
using GreenDonut.DependencyInjection;
using HotChocolate.Execution;
using HotChocolate.Execution.Caching;
using HotChocolate.Execution.Configuration;
using HotChocolate.Execution.DependencyInjection;
using HotChocolate.Execution.Internal;
using HotChocolate.Execution.Processing;
using HotChocolate.Execution.Processing.Tasks;
using HotChocolate.Fetching;
using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
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
        int maximumRetained = ResultPoolDefaults.MaximumRetained,
        int maximumArrayCapacity = ResultPoolDefaults.MaximumAllowedCapacity)
    {
        services.TryAddSingleton(_ => new ObjectResultPool(maximumRetained, maximumArrayCapacity));
        services.TryAddSingleton(_ => new ListResultPool(maximumRetained, maximumArrayCapacity));
        services.TryAddSingleton<ResultPool>();
        return services;
    }

    internal static IServiceCollection TryAddResolverTaskPool(
        this IServiceCollection services,
        int maximumRetained = 128)
    {
        services.TryAddSingleton<ObjectPool<ResolverTask>>(
            _ => new ExecutionTaskPool<ResolverTask, ResolverTaskPoolPolicy>(
                new ResolverTaskPoolPolicy(),
                maximumRetained));
        services.TryAddSingleton<IFactory<ResolverTask>>(
            sp => new PooledServiceFactory<ResolverTask>(
                sp.GetRequiredService<ObjectPool<ResolverTask>>()));
        return services;
    }

    internal static IServiceCollection TryAddOperationCompilerPool(
        this IServiceCollection services)
    {
        services.TryAddSingleton<ObjectPool<OperationCompiler>>(
            sp => new OperationCompilerPool(sp.GetRequiredService<InputParser>()));
        return services;
    }

    internal static IServiceCollection TryAddOperationContextPool(
        this IServiceCollection services)
    {
        services.TryAddSingleton<IFactory<OperationContext>, OperationContextFactory>();
        services.TryAddSingleton<IFactory<OperationContextOwner>, OperationContextOwnerFactory>();

        services.TryAddSingleton(
            sp =>
            {
                var provider = sp.GetRequiredService<ObjectPoolProvider>();
                var policy = new OperationContextPooledObjectPolicy(
                    sp.GetRequiredService<IFactory<OperationContext>>());
                return provider.Create(policy);
            });

        return services;
    }

    internal static IServiceCollection TryAddDeferredWorkStatePool(
        this IServiceCollection services)
    {
        services.TryAddSingleton(
            sp =>
            {
                var provider = sp.GetRequiredService<ObjectPoolProvider>();
                var policy = new DeferredWorkStatePooledObjectPolicy();
                return provider.Create(policy);
            });

        services.TryAddScoped<IFactory<DeferredWorkStateOwner>, DeferredWorkStateOwnerFactory>();

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
        services.TryAddSingleton(sp => new InputFormatter(sp.GetRequiredService<ITypeConverter>()));
        return services;
    }

    internal static IServiceCollection TryAddInputParser(
        this IServiceCollection services)
    {
        services.TryAddSingleton(sp => new InputParser(sp.GetRequiredService<ITypeConverter>()));
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
        return services;
    }

    internal static IServiceCollection TryAddDefaultDocumentHashProvider(
        this IServiceCollection services)
    {
        services.TryAddSingleton<IDocumentHashProvider>(
            _ => new MD5DocumentHashProvider(HashFormat.Hex));
        return services;
    }

    internal static IServiceCollection TryAddDefaultBatchDispatcher(
        this IServiceCollection services)
    {
        services.TryAddScoped<IBatchHandler, BatchScheduler>();
        services.TryAddScoped<IBatchScheduler, AutoBatchScheduler>();
        services.TryAddScoped<IBatchDispatcher>(sp => sp.GetRequiredService<IBatchHandler>());
        return services;
    }

    internal static IServiceCollection TryAddDefaultDataLoaderRegistry(
        this IServiceCollection services)
    {
        services.TryAddDataLoaderCore();
        services.RemoveAll<IDataLoaderScope>();
        services.TryAddSingleton<DataLoaderScopeHolder>();
        services.TryAddScoped<IDataLoaderScopeFactory, ExecutionDataLoaderScopeFactory>();
        services.TryAddScoped<IDataLoaderScope>(
            sp => sp.GetRequiredService<DataLoaderScopeHolder>().GetOrCreateScope(sp));
        return services;
    }

    internal static IServiceCollection TryAddDataLoaderParameterExpressionBuilder(
        this IServiceCollection services)
        => services.TryAddParameterExpressionBuilder<DataLoaderParameterExpressionBuilder>();

    internal static IServiceCollection TryAddParameterExpressionBuilder<T>(
        this IServiceCollection services)
        where T : class, IParameterExpressionBuilder
    {
        if (!services.IsImplementationTypeRegistered<T>())
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
        private readonly IFactory<OperationContext> _contextFactory;

        public OperationContextPooledObjectPolicy(IFactory<OperationContext> contextFactory)
        {
            _contextFactory = contextFactory ??
                throw new ArgumentNullException(nameof(contextFactory));
        }

        public override OperationContext Create()
            => _contextFactory.Create();

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

    private sealed class DeferredWorkStatePooledObjectPolicy : PooledObjectPolicy<DeferredWorkState>
    {
        public override DeferredWorkState Create() => new();

        public override bool Return(DeferredWorkState obj)
        {
            obj.Reset();
            return true;
        }
    }
}
