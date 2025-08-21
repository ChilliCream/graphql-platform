using System.Diagnostics.CodeAnalysis;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Execution;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class CoreFusionGatewayBuilderExtensions
{
    /// <summary>
    /// Adds a custom Node ID parser.
    /// </summary>
    /// <typeparam name="T">
    /// The type of parser.
    /// </typeparam>
    /// <param name="builder">
    /// The fusion gateway builder.
    /// </param>
    /// <returns>
    /// The fusion gateway builder.
    /// </returns>
    public static IFusionGatewayBuilder AddNodeIdParser<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        this IFusionGatewayBuilder builder)
        where T : class, INodeIdParser
    {
        builder.Services.RemoveAll<INodeIdParser>();
        builder.Services.AddSingleton<INodeIdParser, T>();

        return builder;
    }
}
