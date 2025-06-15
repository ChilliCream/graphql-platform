using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Execution;
using HotChocolate.Language;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class CoreFusionGatewayBuilderExtensions
{
    public static IFusionGatewayBuilder ModifyParserOptions(
        this IFusionGatewayBuilder builder,
        Action<FusionParserOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        return Configure(
            builder,
            options => options.ParserOptionsModifiers.Add(configure));
    }
}
