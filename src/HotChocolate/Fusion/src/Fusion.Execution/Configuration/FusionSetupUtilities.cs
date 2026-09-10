using HotChocolate.Fusion.Types.Metadata;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Configuration;

#pragma warning disable CS0618 // Shared configuration also supports legacy-only builders.

/// <summary>
/// Provides helpers to configure core configuration properties.
/// </summary>
public static class FusionSetupUtilities
{
    /// <summary>
    /// Gets the max supported fusion version.
    /// </summary>
    public static Version Version { get; } = new(2, 0, 0, 0);

    public static TBuilder Configure<TBuilder>(
        TBuilder builder,
        Action<FusionRouterSetup> configure)
        where TBuilder : IFusionGatewayBuilder
    {
        builder.Services.Configure(builder.Name, configure);
        return builder;
    }

    /// <summary>
    /// Sets the schema environment properties for the router.
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
    /// Clears the pipeline of the router builder.
    /// </summary>
    /// <param name="builder">
    /// The builder to clear the pipeline of.
    /// </param>
    public static void ClearPipeline(IFusionGatewayBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        Configure(builder, static o => o.PipelineModifiers.Clear());
    }
}
