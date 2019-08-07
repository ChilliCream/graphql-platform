using System;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.DataLoader;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection.Extensions;
using GreenDonut;

namespace HotChocolate
{
    public static class DataLoaderServiceCollectionExtensions
    {
        public static IServiceCollection AddDataLoader<T>(
            this IServiceCollection services)
            where T : class, IDataLoader
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services
                .AddDataLoaderRegistry()
                .TryAddTransient<T>();

            return services;
        }

        public static IServiceCollection AddDataLoader<TService>(
            this IServiceCollection services,
            Func<IServiceProvider, TService> factory)
            where TService : class, IDataLoader
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (factory is null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            services
                .AddDataLoaderRegistry()
                .TryAddTransient<TService>(factory);

            return services;
        }

        public static IServiceCollection AddDataLoader<TService, TImplementation>(
            this IServiceCollection services)
            where TService : class, IDataLoader
            where TImplementation : class, TService
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services
                .AddDataLoaderRegistry()
                .TryAddTransient<TService, TImplementation>();

            return services;
        }

        public static IServiceCollection AddDataLoaderRegistry(
            this IServiceCollection services)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.TryAddScoped<IDataLoaderRegistry, DataLoaderRegistry>();
            services.TryAddScoped<IBatchOperation>(sp =>
            {
                var batchOperation = new DataLoaderBatchOperation();

                foreach (IDataLoaderRegistry registry in
                    sp.GetServices<IDataLoaderRegistry>())
                {
                    registry.Subscribe(batchOperation);
                }

                return batchOperation;
            });
            return services;
        }
    }
}
