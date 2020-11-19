using System;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using GreenDonut;
using HotChocolate.DataLoader;
using HotChocolate.Execution;
using HotChocolate.Execution.Caching;
using HotChocolate.Execution.Configuration;
using HotChocolate.Execution.Internal;
using HotChocolate.Execution.Processing;
using HotChocolate.Fetching;
using HotChocolate.Language;
using HotChocolate.Types.Relay;
using HotChocolate.Utilities;

namespace Microsoft.Extensions.DependencyInjection
{
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
            int maximumRetained = 256)
        {
            services.TryAddSingleton<ObjectPool<ResolverTask>>(
                _ => new ResolverTaskPool(
                    maximumRetained));
            return services;
        }

        internal static IServiceCollection TryAddOperationContextPool(
            this IServiceCollection services,
            int maximumRetained = -1)
        {
            if (maximumRetained < 1)
            {
                maximumRetained = Environment.ProcessorCount * 2;
            }

            services.TryAddTransient<OperationContext>();
            services.TryAddSingleton<ObjectPool<OperationContext>>(
                sp => new DefaultObjectPool<OperationContext>(
                    new OperationContextPoolPolicy(sp.GetRequiredService<OperationContext>),
                    maximumRetained));
            return services;
        }

        internal static IServiceCollection TryAddTypeConverter(
            this IServiceCollection services)
        {
            services.TryAddSingleton<ITypeConverter>(
                sp => new DefaultTypeConverter(sp.GetServices<IChangeTypeProvider>()));
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
    }
}
