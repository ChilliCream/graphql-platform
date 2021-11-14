using System;
using GreenDonut;
using HotChocolate.Execution.Configuration;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Options;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

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

        return builder.AddDiagnosticEventListener(
            sp => new ApolloTracingDiagnosticEventListener(
                tracingPreference,
                timestampProvider ?? sp.GetService<ITimestampProvider>()));
    }

    public static IRequestExecutorBuilder AddDiagnosticEventListener<T>(
        this IRequestExecutorBuilder builder)
        where T : class
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (typeof(IDataLoaderDiagnosticEventListener).IsAssignableFrom(typeof(T)))
        {
            builder.Services.TryAddSingleton<T>();
            builder.Services.AddSingleton(
                s => (IDataLoaderDiagnosticEventListener)s.GetService<T>());
        }
        else if (typeof(IExecutionDiagnosticEventListener).IsAssignableFrom(typeof(T)))
        {
            builder.Services.TryAddSingleton<T>();
            builder.ConfigureSchemaServices(
                s => s.AddSingleton(
                    sp => (IExecutionDiagnosticEventListener)sp.GetApplicationService<T>()));
        }
        else
        {
            throw new NotSupportedException("The diagnostic listener is not supported.");
        }

        return builder;
    }

    public static IRequestExecutorBuilder AddDiagnosticEventListener<T>(
        this IRequestExecutorBuilder builder,
        Func<IServiceProvider, T> diagnosticEventListener)
        where T : class
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (diagnosticEventListener is null)
        {
            throw new ArgumentNullException(nameof(diagnosticEventListener));
        }

        if (typeof(IDataLoaderDiagnosticEventListener).IsAssignableFrom(typeof(T)))
        {
            builder.Services.AddSingleton(
                s => (IDataLoaderDiagnosticEventListener)diagnosticEventListener(s));
        }
        else if (typeof(IExecutionDiagnosticEventListener).IsAssignableFrom(typeof(T)))
        {
            builder.ConfigureSchemaServices(
                s => s.AddSingleton(
                    sp => (IExecutionDiagnosticEventListener)diagnosticEventListener(
                        sp.GetCombinedServices())));
        }
        else
        {
            throw new NotSupportedException("The diagnostic listener is not supported.");
        }

        return builder;
    }
}
