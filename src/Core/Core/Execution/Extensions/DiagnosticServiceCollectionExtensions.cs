using System;
using HotChocolate.Execution.Instrumentation;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution
{
    public static class DiagnosticServiceCollectionExtensions
    {
        public static IServiceCollection AddDiagnosticObserver<TListener>(
            this IServiceCollection services)
            where TListener : class, IDiagnosticObserver
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            return services.AddSingleton<IDiagnosticObserver, TListener>();
        }

        public static IServiceCollection AddDiagnosticObserver<TObserver>(
            this IServiceCollection services,
            TObserver observer)
            where TObserver : class, IDiagnosticObserver
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (observer == null)
            {
                throw new ArgumentNullException(nameof(observer));
            }

            return services.AddSingleton<IDiagnosticObserver>(observer);
        }
    }
}
