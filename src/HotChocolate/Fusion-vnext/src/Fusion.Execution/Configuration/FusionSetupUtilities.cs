using HotChocolate.Fusion.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Configuration;

/// <summary>
/// Provides helpers to configure core configuration properties.
/// </summary>
public static class FusionSetupUtilities
{
    internal static IFusionGatewayBuilder Configure(
        IFusionGatewayBuilder builder,
        Action<FusionGatewaySetup> configure)
    {
        builder.Services.Configure(builder.Name, configure);
        return builder;
    }

    /// <summary>
    /// Sets the schema environment properties for the gateway.
    /// </summary>
    /// <param name="builder">
    /// The builder to configure.
    /// </param>
    /// <param name="appId">
    /// The application identifier.
    /// </param>
    /// <param name="environmentName">
    /// The environment name.
    /// </param>
    public static void SetSchemaEnvironment(
        IFusionGatewayBuilder builder,
        string appId,
        string environmentName)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(appId);
        ArgumentException.ThrowIfNullOrEmpty(environmentName);

        Configure(
            builder,
            setup => setup.SchemaFeaturesModifiers.Add(
                (_, features) => features.Set(new SchemaEnvironment(appId, environmentName))));
    }

    /// <summary>
    /// Clears the pipeline of the <see cref="IFusionGatewayBuilder"/>.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IFusionGatewayBuilder"/> to clear the pipeline of.
    /// </param>
    public static void ClearPipeline(IFusionGatewayBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        Configure(builder, static o => o.PipelineModifiers.Clear());
    }
}
