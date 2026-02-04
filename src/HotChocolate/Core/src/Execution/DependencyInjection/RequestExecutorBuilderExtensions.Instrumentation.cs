using GreenDonut;
using HotChocolate;
using HotChocolate.Execution.Configuration;
using HotChocolate.Execution.Instrumentation;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static partial class RequestExecutorBuilderExtensions
{
    /// <summary>
    /// Registers a diagnostic event listener.
    /// </summary>
    /// <param name="builder">
    /// The request executor builder.
    /// </param>
    /// <typeparam name="T">
    /// The type of the diagnostic event listener.
    /// </typeparam>
    /// <returns>
    /// The request executor builder.
    /// </returns>
    /// <exception cref="NotSupportedException">
    /// The <typeparamref name="T"/> is not a recognized diagnostic event listener.
    /// </exception>
    /// <remarks>
    /// The <typeparamref name="T"/> will be activated with the <see cref="IServiceProvider"/> of the schema services.
    /// If your <typeparamref name="T"/> needs to access application services you need to
    /// make the services available in the schema services via <see cref="AddApplicationService"/>.
    /// </remarks>
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

    /// <summary>
    /// Registers a diagnostic event listener.
    /// </summary>
    /// <param name="builder">
    /// The request executor builder.
    /// </param>
    /// <param name="factory">
    /// The factory to produce the diagnostic event listener.
    /// </param>
    /// <typeparam name="T">
    /// The type of the diagnostic event listener.
    /// </typeparam>
    /// <returns>
    /// The request executor builder.
    /// </returns>
    /// <exception cref="NotSupportedException">
    /// The service returned from the <paramref name="factory"/> is not a recognized diagnostic event listener.
    /// </exception>
    /// <remarks>
    /// The <see cref="IServiceProvider"/> passed to the <paramref name="factory"/>
    /// is for the schema services. If you need to access application services
    /// you need to either make the services available in the schema services
    /// via <see cref="AddApplicationService"/> or use
    /// <see cref="ExecutionServiceProviderExtensions.GetRootServiceProvider(IServiceProvider)"/>
    /// to access the application services from within the schema service provider.
    /// </remarks>
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
