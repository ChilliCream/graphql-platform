using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.ObjectPool;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Processing;
using HotChocolate.Utilities;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class InternalSchemaServiceCollectionExtensions
    {
        internal static IServiceCollection TryAddOperationExecutors(
            this IServiceCollection services)
        {
            services.TryAddSingleton<QueryExecutor>();
            services.TryAddSingleton<MutationExecutor>();
            services.TryAddSingleton(
                sp => new SubscriptionExecutor(
                    sp.GetApplicationService<ObjectPool<OperationContext>>(),
                    sp.GetRequiredService<QueryExecutor>(),
                    sp.GetRequiredService<IDiagnosticEvents>()));
            return services;
        }

        internal static IServiceCollection TryAddDiagnosticEvents(
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

        internal static IServiceCollection TryAddTimespanProvider(
            this IServiceCollection services)
        {
            services.TryAddSingleton<ITimestampProvider, DefaultTimestampProvider>();
            return services;
        }

        public static T GetApplicationService<T>(this IServiceProvider services) =>
            services.GetApplicationServices().GetRequiredService<T>();

        public static IServiceProvider GetApplicationServices(this IServiceProvider services) =>
            services.GetRequiredService<IApplicationServiceProvider>();

        /// <summary>
        /// Gets a service provided that represents the combined services from the schema services
        /// and application services.
        /// </summary>
        public static IServiceProvider GetCombinedServices(this IServiceProvider services) =>
            services is CombinedServiceProvider combined
                ? combined
                : new CombinedServiceProvider(
                    services.GetApplicationServices(),
                    services);
    }
}
