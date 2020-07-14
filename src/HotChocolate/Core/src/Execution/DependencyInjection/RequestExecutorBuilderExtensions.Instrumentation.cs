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
            if (tracingPreference == TracingPreference.Never)
            {
                return builder;
            }

            return AddDiagnosticEventListener(
                builder, 
                sp => new ApolloTracingDiagnosticEventListener(
                    tracingPreference,
                    timestampProvider ?? sp.GetService<ITimestampProvider>()));
        }

        public static IRequestExecutorBuilder AddDiagnosticEventListener<T>(
            this IRequestExecutorBuilder builder)
            where T : class, IDiagnosticEventListener
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services.AddSingleton<IDiagnosticEventListener, T>();
            return builder;
        }

        public static IRequestExecutorBuilder AddDiagnosticEventListener<T>(
            this IRequestExecutorBuilder builder,
            Func<IServiceProvider, T> diagnosticEventListener)
            where T : IDiagnosticEventListener
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (diagnosticEventListener == null)
            {
                throw new ArgumentNullException(nameof(diagnosticEventListener));
            }

            builder.Services.AddSingleton<IDiagnosticEventListener>(
                sp => diagnosticEventListener(sp));
            return builder;
        }
    }
}