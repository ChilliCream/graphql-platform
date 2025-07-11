using GreenDonut;
using HotChocolate;
using HotChocolate.Execution.Configuration;
using HotChocolate.Execution.Instrumentation;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static partial class RequestExecutorBuilderExtensions
{
    public static IRequestExecutorBuilder AddDiagnosticEventListener<T>(
        this IRequestExecutorBuilder builder)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (typeof(IExecutionDiagnosticEventListener).IsAssignableFrom(typeof(T)))
        {
            builder.Services.TryAddSingleton<T>();
            builder.ConfigureSchemaServices(
                s => s.AddSingleton(
                    sp => (IExecutionDiagnosticEventListener)sp.GetRootServiceProvider().GetRequiredService<T>()));
        }
        else if (typeof(IDataLoaderDiagnosticEventListener).IsAssignableFrom(typeof(T)))
        {
            builder.Services.TryAddSingleton<T>();
            builder.Services.AddSingleton(s => (IDataLoaderDiagnosticEventListener)s.GetRequiredService<T>());
        }
        else if (typeof(T).IsDefined(typeof(DiagnosticEventSourceAttribute), true))
        {
            builder.Services.TryAddSingleton<T>();

            builder.ConfigureSchemaServices(static s =>
            {
                var attribute = typeof(T).GetCustomAttributes(typeof(DiagnosticEventSourceAttribute), true).First();
                var listener = ((DiagnosticEventSourceAttribute)attribute).Listener;
                s.AddSingleton(listener, sp => sp.GetRootServiceProvider().GetRequiredService<T>());
            });
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
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(diagnosticEventListener);

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
            var attribute =
                (DiagnosticEventSourceAttribute)typeof(T)
                    .GetCustomAttributes(typeof(DiagnosticEventSourceAttribute), true)
                    .First();

            if (attribute.IsSchemaService)
            {
                builder.ConfigureSchemaServices(s =>
                {
                    var listener = attribute.Listener;
                    s.AddSingleton(listener, sp => diagnosticEventListener(sp.GetCombinedServices()));
                });
            }
            else
            {
                var listener = attribute.Listener;
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
