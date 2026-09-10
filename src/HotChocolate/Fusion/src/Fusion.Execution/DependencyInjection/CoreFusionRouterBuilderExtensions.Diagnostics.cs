using System.Diagnostics.CodeAnalysis;
using HotChocolate.Fusion.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

#pragma warning disable CS0618 // Both builder surfaces share the same configuration operations.

public static partial class CoreFusionRouterBuilderExtensions
{
    /// <summary>
    /// Adds a diagnostic event listener activated using the schema services,
    /// or the application services for an application-level diagnostic event source.
    /// </summary>
    public static IFusionRouterBuilder AddDiagnosticEventListener<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        this IFusionRouterBuilder builder)
        where T : class
    {
        CoreFusionGatewayBuilderExtensions.AddDiagnosticEventListener<T>(builder);
        return builder;
    }

    /// <summary>
    /// Adds a diagnostic event listener created using the schema services,
    /// or the application services for an application-level diagnostic event source.
    /// </summary>
    public static IFusionRouterBuilder AddDiagnosticEventListener<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        this IFusionRouterBuilder builder,
        Func<IServiceProvider, T> factory)
        where T : class
    {
        CoreFusionGatewayBuilderExtensions.AddDiagnosticEventListener(builder, factory);
        return builder;
    }
}
