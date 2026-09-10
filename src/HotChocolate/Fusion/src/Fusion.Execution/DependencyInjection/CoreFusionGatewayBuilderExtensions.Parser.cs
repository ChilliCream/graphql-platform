using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Execution;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class CoreFusionGatewayBuilderExtensions
{
    [Obsolete("Use ModifyParserOptions on IFusionRouterBuilder instead.")]
    public static IFusionGatewayBuilder ModifyParserOptions(
        this IFusionGatewayBuilder builder,
        Action<FusionParserOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        return FusionSetupUtilities.Configure(
            builder,
            options => options.ParserOptionsModifiers.Add(configure));
    }
}
