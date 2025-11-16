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
    public static IFusionGatewayBuilder AddDiagnosticEventListener<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        this IFusionGatewayBuilder builder)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (typeof(IFusionExecutionDiagnosticEventListener).IsAssignableFrom(typeof(T)))
        {
            builder.Services.TryAddSingleton<T>();
            builder.ConfigureSchemaServices(static (_, s) =>
                s.AddSingleton(static sp => (IFusionExecutionDiagnosticEventListener)sp.GetRequiredService<T>()));
        }
        else if (typeof(T).IsDefined(typeof(DiagnosticEventSourceAttribute), true))
        {
            builder.Services.TryAddSingleton<T>();
            builder.ConfigureSchemaServices(static (_, s) =>
            {
                var attribute = typeof(T).GetCustomAttributes(typeof(DiagnosticEventSourceAttribute), true).First();
                var listener = ((DiagnosticEventSourceAttribute)attribute).Listener;
                s.AddSingleton(listener, sp => sp.GetRequiredService<T>());
            });
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
    public static IFusionGatewayBuilder AddDiagnosticEventListener<T>(
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
            var attribute =
                (DiagnosticEventSourceAttribute)typeof(T)
                    .GetCustomAttributes(typeof(DiagnosticEventSourceAttribute), true)
                    .First();

            if (attribute.IsSchemaService)
            {
                builder.ConfigureSchemaServices((_, s) =>
                {
                    var listener = attribute.Listener;
                    s.AddSingleton(listener, factory);
                });
            }
            else
            {
                var listener = attribute.Listener;
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
