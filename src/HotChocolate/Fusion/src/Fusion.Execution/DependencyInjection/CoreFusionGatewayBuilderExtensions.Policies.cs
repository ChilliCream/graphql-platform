using HotChocolate.Fusion.Authorization;
using HotChocolate.Fusion.Authorization.InMemory;
using HotChocolate.Fusion.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class CoreFusionGatewayBuilderExtensions
{
    /// <summary>
    /// Adds in-memory policies whose verdicts are configured in code. Every entry they are
    /// asked about is available from the <see cref="InMemoryPolicyRecorder"/> of the application services.
    /// </summary>
    /// <param name="builder">
    /// The fusion gateway builder.
    /// </param>
    /// <param name="configure">
    /// A delegate that configures the verdicts.
    /// </param>
    /// <returns>
    /// The fusion gateway builder.
    /// </returns>
    public static IFusionGatewayBuilder AddInMemoryPolicies(
        this IFusionGatewayBuilder builder,
        Action<InMemoryPolicyBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        var policies = new InMemoryPolicyBuilder();
        configure(policies);

        builder.Services.TryAddSingleton<InMemoryPolicyRecorder>();

        return builder.ConfigureSchemaServices(
            (sp, sc) =>
            {
                sc.AddSingleton<IPolicyProvider>(
                    policies.Build(sp.GetRequiredService<InMemoryPolicyRecorder>()));
            });
    }
}
