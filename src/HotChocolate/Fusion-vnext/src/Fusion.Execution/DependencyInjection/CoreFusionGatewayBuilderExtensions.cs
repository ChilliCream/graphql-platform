using HotChocolate.Execution;
using HotChocolate.Fusion.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class CoreFusionGatewayBuilderExtensions
{
    private static IFusionGatewayBuilder Configure(
        IFusionGatewayBuilder builder,
        Action<FusionGatewaySetup> configure)
    {
        builder.Services.Configure(configure);
        return builder;
    }
}
