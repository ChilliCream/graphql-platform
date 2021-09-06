using System;
using HotChocolate.Execution.Configuration;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class RequestExecutorBuilderExtensions
    {
        public static IRequestExecutorBuilder AddApolloTracing(
            this IRequestExecutorBuilder builder,
            TracingPreference tracingPreference = TracingPreference.OnDemand,
            ITimestampProvider? timestampProvider = null)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (tracingPreference == TracingPreference.Never)
            {
                return builder;
            }

            return builder.ConfigureSchemaServices(
                s => s.AddSingleton<IExecutionDiagnosticEventListener>(
                    sp => new ApolloTracingDiagnosticEventListener(
                        tracingPreference,
                        timestampProvider ?? sp.GetService<ITimestampProvider>())));
        }

        public static IRequestExecutorBuilder AddDiagnosticEventListener<T>(
            this IRequestExecutorBuilder builder)
            where T : class, IExecutionDiagnosticEventListener
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.ConfigureSchemaServices(s => s.AddSingleton<IExecutionDiagnosticEventListener, T>());
            return builder;
        }

        public static IRequestExecutorBuilder AddDiagnosticEventListener<T>(
            this IRequestExecutorBuilder builder,
            Func<IServiceProvider, T> diagnosticEventListener)
            where T : IExecutionDiagnosticEventListener
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (diagnosticEventListener is null)
            {
                throw new ArgumentNullException(nameof(diagnosticEventListener));
            }

            return builder.ConfigureSchemaServices(
                s => s.AddSingleton<IExecutionDiagnosticEventListener>(
                    sp => diagnosticEventListener(sp.GetCombinedServices())));
        }
    }
}
