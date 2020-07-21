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
using System;
using HotChocolate.Execution.Configuration;

namespace Microsoft.Extensions.DependencyInjection
{
    internal static class InternalSchemaServiceCollectionExtensions
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

        internal static T GetApplicationService<T>(this IServiceProvider services) =>
            services.GetRequiredService<IApplicationServiceProvider>().GetRequiredService<T>();
    }
}
