using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Execution;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class CoreFusionGatewayBuilderExtensions
{
    public static IFusionGatewayBuilder ModifyOptions(
        this IFusionGatewayBuilder builder,
        Action<FusionOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        return FusionSetupUtilities.Configure(
            builder,
            options => options.OptionsModifiers.Add(configure));
    }

    public static IFusionGatewayBuilder ModifyRequestOptions(
        this IFusionGatewayBuilder builder,
        Action<FusionRequestOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        return FusionSetupUtilities.Configure(
            builder,
            options => options.RequestOptionsModifiers.Add(configure));
    }
}
