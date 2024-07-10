#nullable enable

namespace HotChocolate.Types.Relay;

/// <summary>
/// The node id serializer accessor provides access to the node id serializer.
/// </summary>
public interface INodeIdSerializerAccessor
{
    /// <summary>
    /// Gets the node id serializer.
    /// </summary>
    INodeIdSerializer Serializer { get; }
}
