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
            builder.ConfigureSchemaServices(
                static s =>
                {
                    s.TryAddSingleton<T>();
                    s.AddSingleton(static sp => (IExecutionDiagnosticEventListener)sp.GetRequiredService<T>());
                });
        }
        else if (typeof(IDataLoaderDiagnosticEventListener).IsAssignableFrom(typeof(T)))
        {
            builder.Services.TryAddSingleton<T>();
            builder.Services.AddSingleton(
                static s => (IDataLoaderDiagnosticEventListener)s.GetRequiredService<T>());
        }
        else if (typeof(T).IsDefined(typeof(DiagnosticEventSourceAttribute), true))
        {
            var attribute = (DiagnosticEventSourceAttribute)typeof(T).GetCustomAttributes(typeof(DiagnosticEventSourceAttribute), true).First();
            var listener = attribute.Listener;

            if (attribute.IsSchemaService)
            {
                builder.ConfigureSchemaServices(s =>
                {
                    s.TryAddSingleton<T>();
                    s.AddSingleton(listener, sp => sp.GetRequiredService<T>());
                });
            }
            else
            {
                builder.Services.TryAddSingleton<T>();
                builder.Services.AddSingleton(listener, sp => sp.GetRequiredService<T>());
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
        Func<IServiceProvider, T> factory)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(factory);

        if (typeof(IExecutionDiagnosticEventListener).IsAssignableFrom(typeof(T)))
        {
            builder.ConfigureSchemaServices(
                s => s.AddSingleton(sp => (IExecutionDiagnosticEventListener)factory(sp)));
        }
        else if (typeof(IDataLoaderDiagnosticEventListener).IsAssignableFrom(typeof(T)))
        {
            builder.Services.AddSingleton(
                s => (IDataLoaderDiagnosticEventListener)factory(s));
        }
        else if (typeof(T).IsDefined(typeof(DiagnosticEventSourceAttribute), true))
        {
            var attribute = (DiagnosticEventSourceAttribute)typeof(T).GetCustomAttributes(typeof(DiagnosticEventSourceAttribute), true).First();
            var listener = attribute.Listener;

            if (attribute.IsSchemaService)
            {
                builder.ConfigureSchemaServices(s => s.AddSingleton(listener, factory));
            }
            else
            {
                builder.Services.AddSingleton(listener, factory);
            }
        }
        else
        {
            throw new NotSupportedException("The diagnostic listener is not supported.");
        }

        return builder;
    }
}
