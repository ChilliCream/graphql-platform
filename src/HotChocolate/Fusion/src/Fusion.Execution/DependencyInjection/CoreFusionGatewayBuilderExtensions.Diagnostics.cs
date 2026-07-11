using System.Diagnostics.CodeAnalysis;
using HotChocolate;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Diagnostics;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class CoreFusionGatewayBuilderExtensions
{
    /// <summary>
    /// Adds a diagnostic event listener to the fusion gateway.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the diagnostic event listener.
    /// </typeparam>
    /// <param name="builder">
    /// The fusion gateway builder.
    /// </param>
    /// <returns>
    /// The fusion gateway builder.
    /// </returns>
    /// <remarks>
    /// The <typeparamref name="T"/> will be activated with the <see cref="IServiceProvider"/> of the schema services.
    /// If your <typeparamref name="T"/> needs to access application services you need to
    /// make the services available in the schema services via <see cref="AddApplicationService"/>.
    /// </remarks>
    public static IFusionGatewayBuilder AddDiagnosticEventListener<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        this IFusionGatewayBuilder builder)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (typeof(IFusionExecutionDiagnosticEventListener).IsAssignableFrom(typeof(T)))
        {
            builder.ConfigureSchemaServices(static (_, s) =>
            {
                s.TryAddSingleton<T>();
                s.AddSingleton(static sp => (IFusionExecutionDiagnosticEventListener)sp.GetRequiredService<T>());
            });
        }
        else if (typeof(T).IsDefined(typeof(DiagnosticEventSourceAttribute), true))
        {
            var attribute = (DiagnosticEventSourceAttribute)typeof(T).GetCustomAttributes(typeof(DiagnosticEventSourceAttribute), true).First();
            var listener = attribute.Listener;

            if (attribute.IsSchemaService)
            {
                builder.ConfigureSchemaServices((_, s) =>
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
    /// Adds a diagnostic event listener to the fusion gateway.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the diagnostic event listener.
    /// </typeparam>
    /// <param name="factory">
    /// The factory to create the diagnostic event listener.
    /// </param>
    /// <param name="builder">
    /// The fusion gateway builder.
    /// </param>
    /// <returns>
    /// The fusion gateway builder.
    /// </returns>
    /// <remarks>
    /// The <see cref="IServiceProvider"/> passed to the <paramref name="factory"/>
    /// is for the schema services. If you need to access application services
    /// you need to either make the services available in the schema services
    /// via <see cref="AddApplicationService"/> or use
    /// <see cref="ExecutionServiceProviderExtensions.GetRootServiceProvider(IServiceProvider)"/>
    /// to access the application services from within the schema service provider.
    /// </remarks>
    public static IFusionGatewayBuilder AddDiagnosticEventListener<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        this IFusionGatewayBuilder builder,
        Func<IServiceProvider, T> factory)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(factory);

        if (typeof(IFusionExecutionDiagnosticEventListener).IsAssignableFrom(typeof(T)))
        {
            builder.ConfigureSchemaServices((_, s) =>
                s.AddSingleton(sp => (IFusionExecutionDiagnosticEventListener)factory(sp)));
        }
        else if (typeof(T).IsDefined(typeof(DiagnosticEventSourceAttribute), true))
        {
            var attribute = (DiagnosticEventSourceAttribute)typeof(T).GetCustomAttributes(typeof(DiagnosticEventSourceAttribute), true).First();
            var listener = attribute.Listener;

            if (attribute.IsSchemaService)
            {
                builder.ConfigureSchemaServices((_, s) => s.AddSingleton(listener, factory));
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
