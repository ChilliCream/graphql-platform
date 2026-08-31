using HotChocolate.Fusion.Diagnostics;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Policies.Rego;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides extension methods to register Rego policy support on a Fusion gateway.
/// </summary>
public static class RegoFusionGatewayBuilderExtensions
{
    /// <summary>
    /// Registers the Rego policy provider so that policies packaged as Rego take effect on the
    /// gateway.
    /// </summary>
    /// <param name="builder">The Fusion gateway builder.</param>
    /// <returns>The Fusion gateway builder.</returns>
    public static IFusionGatewayBuilder AddRegoPolicies(this IFusionGatewayBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.ConfigureSchemaServices(
            static (_, services) =>
            {
                // Exactly one policy provider is active per gateway: the first registration wins,
                // so calling this alongside another policy provider registration is a no-op for
                // whichever one runs second.
                services.TryAddSingleton<IPolicyProvider>(
                    static sp => new RegoPolicyProvider(
                        sp.GetRequiredService<IFusionExecutionDiagnosticEvents>()));
            });
    }
}
