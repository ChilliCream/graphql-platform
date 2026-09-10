using HotChocolate.Fusion.Diagnostics;
using HotChocolate.Fusion.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

// The legacy extension surface holds the shared operations until the compatibility surface
// is removed. Calling it here forwards through the same configuration pipeline.
#pragma warning disable CS0618

public static class DiagnosticsFusionRouterBuilderExtensions
{
    public static IFusionRouterBuilder AddInstrumentation(
        this IFusionRouterBuilder builder,
        Action<InstrumentationOptions>? options = null)
    {
        DiagnosticsFusionGatewayBuilderExtensions.AddInstrumentation(builder, options);
        return builder;
    }

    public static IFusionRouterBuilder AddInstrumentation(
        this IFusionRouterBuilder builder,
        Action<IServiceProvider, InstrumentationOptions> options)
    {
        DiagnosticsFusionGatewayBuilderExtensions.AddInstrumentation(builder, options);
        return builder;
    }
}
