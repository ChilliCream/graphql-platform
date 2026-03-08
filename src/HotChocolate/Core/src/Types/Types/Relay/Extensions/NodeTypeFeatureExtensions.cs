using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Types.Relay;

/// <summary>
/// Extensions for the <see cref="NodeTypeFeature"/>.
/// </summary>
internal static class NodeTypeFeatureExtensions
{
    /// <summary>
    /// Tries to get the node resolver for the given type.
    /// </summary>
    /// <param name="type">The type to get the node resolver for.</param>
    /// <param name="nodeResolver">The node resolver for the given type.</param>
    /// <returns>True if the node resolver was found, false otherwise.</returns>
    public static bool TryGetNodeResolver(this ObjectType type, [NotNullWhen(true)] out NodeResolverInfo? nodeResolver)
    {
        if (type.Features.TryGet<NodeTypeFeature>(out var feature) && feature.NodeResolver is not null)
        {
            nodeResolver = feature.NodeResolver;
            return true;
        }

        nodeResolver = null;
        return false;
    }

    /// <summary>
    /// Sets the node resolver for the given type.
    /// </summary>
    /// <param name="type">The type to set the node resolver for.</param>
    /// <param name="nodeResolver">The node resolver to set for the given type.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the node resolver feature is not available on the type.
    /// </exception>
    public static void SetNodeResolver(
        this ObjectType type,
        NodeResolverInfo nodeResolver)
    {
        ArgumentNullException.ThrowIfNull(nodeResolver);

        var feature = type.Features.Get<NodeTypeFeature>();

        if (feature is null)
        {
            throw new InvalidOperationException(
                "The node resolver feature is not available on the type. "
                + "Please ensure that the NodeTypeFeature is registered before "
                + "setting a node resolver.");
        }

        feature.NodeResolver = nodeResolver;
    }
}
