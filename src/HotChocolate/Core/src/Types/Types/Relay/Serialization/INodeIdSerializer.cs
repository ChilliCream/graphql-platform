#nullable enable
namespace HotChocolate.Types.Relay;

/// <summary>
/// Represents the relay node id serializer.
/// </summary>
public interface INodeIdSerializer
{
    /// <summary>
    /// Formats the internal id to a relay id.
    /// </summary>
    /// <param name="typeName">
    /// The name of the type for which the id is formatted.
    /// </param>
    /// <param name="internalId">
    /// The internal id that shall be formatted.
    /// </param>
    /// <returns>
    /// Returns the formatted relay id.
    /// </returns>
    string Format(string typeName, object internalId);

    /// <summary>
    /// Parses the relay id to an internal id.
    /// </summary>
    /// <param name="formattedId">
    /// The relay id that shall be parsed.
    /// </param>
    /// <param name="runtimeTypeLookup">
    /// The runtime type lookup that is used to resolve the runtime type of the encoded id.
    /// </param>
    /// <returns>
    /// Returns the parsed internal id.
    /// </returns>
    NodeId Parse(string formattedId, INodeIdRuntimeTypeLookup runtimeTypeLookup);

    /// <summary>
    /// Parses the relay id to an internal id.
    /// </summary>
    /// <param name="formattedId">
    /// The relay id that shall be parsed.
    /// </param>
    /// <param name="runtimeType">
    /// The runtime type for which the id shall be parsed.
    /// </param>
    /// <returns>
    /// Returns the parsed internal id.
    /// </returns>
    NodeId Parse(string formattedId, Type runtimeType);
}
