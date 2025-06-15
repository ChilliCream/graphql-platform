using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Execution;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class CoreFusionGatewayBuilderExtensions
{
    public static IFusionGatewayBuilder ModifyRequestOptions(
        this IFusionGatewayBuilder builder,
        Action<FusionRequestOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        return Configure(
            builder,
            options => options.RequestOptionsModifiers.Add(configure));
    }
}
