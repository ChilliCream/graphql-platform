using System.Diagnostics.CodeAnalysis;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Diagnostics;

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
        where T : class, IFusionExecutionDiagnosticEventListener
    {
        ArgumentNullException.ThrowIfNull(builder);

        return ConfigureSchemaServices(
            builder,
            (_, services) => services.AddSingleton<IFusionExecutionDiagnosticEventListener, T>());
    }

    /// <summary>
    /// Adds a diagnostic event listener to the fusion gateway.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the diagnostic event listener.
    /// </typeparam>
    /// <param name="diagnosticEventListener">
    /// The diagnostic event listener.
    /// </param>
    /// <param name="builder">
    /// The fusion gateway builder.
    /// </param>
    /// <returns>
    /// The fusion gateway builder.
    /// </returns>
    public static IFusionGatewayBuilder AddDiagnosticEventListener<T>(
        this IFusionGatewayBuilder builder,
        Func<IServiceProvider, T> diagnosticEventListener)
        where T : class, IFusionExecutionDiagnosticEventListener
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(diagnosticEventListener);

        return ConfigureSchemaServices(
            builder,
            (_, services) => services.AddSingleton(diagnosticEventListener));
    }
}
