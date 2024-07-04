using System;
using GreenDonut;
using HotChocolate;
using HotChocolate.Execution.Configuration;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Options;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static partial class RequestExecutorBuilderExtensions
{
    public static IRequestExecutorBuilder AddDiagnosticEventListener<T>(
        this IRequestExecutorBuilder builder)
        where T : class
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (typeof(IExecutionDiagnosticEventListener).IsAssignableFrom(typeof(T)))
        {
            builder.Services.TryAddSingleton<T>();
            builder.ConfigureSchemaServices(
                s => s.AddSingleton(
                    sp => (IExecutionDiagnosticEventListener)sp.GetApplicationService<T>()));
        }
        else if (typeof(IDataLoaderDiagnosticEventListener).IsAssignableFrom(typeof(T)))
        {
            builder.Services.TryAddSingleton<T>();
            builder.Services.AddSingleton(s => (IDataLoaderDiagnosticEventListener)s.GetRequiredService<T>());
        }
        else if (typeof(T).IsDefined(typeof(DiagnosticEventSourceAttribute), true))
        {
            builder.Services.TryAddSingleton<T>();

            foreach (var attribute in
                typeof(T).GetCustomAttributes(typeof(DiagnosticEventSourceAttribute), true))
            {
                var listener = ((DiagnosticEventSourceAttribute)attribute).Listener;
                builder.Services.AddSingleton(listener, s => s.GetRequiredService<T>());
            }
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

        if (typeof(IExecutionDiagnosticEventListener).IsAssignableFrom(typeof(T)))
        {
            builder.ConfigureSchemaServices(
                s => s.AddSingleton(
                    sp => (IExecutionDiagnosticEventListener)diagnosticEventListener(
                        sp.GetCombinedServices())));
        }
        else if (typeof(IDataLoaderDiagnosticEventListener).IsAssignableFrom(typeof(T)))
        {
            builder.Services.AddSingleton(
                s => (IDataLoaderDiagnosticEventListener)diagnosticEventListener(s));
        }
        else if (typeof(T).IsDefined(typeof(DiagnosticEventSourceAttribute), true))
        {
            foreach (var attribute in
                typeof(T).GetCustomAttributes(typeof(DiagnosticEventSourceAttribute), true))
            {
                var listener = ((DiagnosticEventSourceAttribute)attribute).Listener;
                builder.Services.AddSingleton(listener, diagnosticEventListener);
            }
        }
        else
        {
            throw new NotSupportedException("The diagnostic listener is not supported.");
        }

        return builder;
    }
}
