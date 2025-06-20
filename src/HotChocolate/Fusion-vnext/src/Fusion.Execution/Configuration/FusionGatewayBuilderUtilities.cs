using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Configuration;

/// <summary>
/// Utility methods for <see cref="IFusionGatewayBuilder"/>.
/// </summary>
public static class FusionGatewayBuilderUtilities
{
    /// <summary>
    /// Clears the pipeline of the <see cref="IFusionGatewayBuilder"/>.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IFusionGatewayBuilder"/> to clear the pipeline of.
    /// </param>
    public static void ClearPipeline(IFusionGatewayBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        CoreFusionGatewayBuilderExtensions.Configure(builder, static o => o.PipelineModifiers.Clear());
    }
}
