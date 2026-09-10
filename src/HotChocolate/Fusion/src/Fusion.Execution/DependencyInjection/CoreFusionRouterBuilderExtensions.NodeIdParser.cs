using System.Diagnostics.CodeAnalysis;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Execution;

namespace Microsoft.Extensions.DependencyInjection;

#pragma warning disable CS0618 // Both builder surfaces share the same configuration operations.

public static partial class CoreFusionRouterBuilderExtensions
{
    /// <summary>
    /// Adds a custom node ID parser to the router schema services.
    /// </summary>
    public static IFusionRouterBuilder AddNodeIdParser<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        this IFusionRouterBuilder builder)
        where T : class, INodeIdParser
    {
        CoreFusionGatewayBuilderExtensions.AddNodeIdParser<T>(builder);
        return builder;
    }
}
