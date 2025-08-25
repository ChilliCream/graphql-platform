using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Fusion.Execution;

/// <summary>
/// This parser is used to extract the type information from node IDs, enabling the gateway
/// to route node field queries to the appropriate source schema that owns the entity.
/// The parser only extracts the type name - it does not perform full ID deserialization
/// or validation beyond what's needed for routing decisions.
/// </summary>
public interface INodeIdParser
{
    /// <summary>
    /// Attempts to parse the type name from a Relay global node ID.
    /// This method is used by the gateway to determine which downstream schema
    /// should handle a node field query based on the entity type encoded in the ID.
    /// </summary>
    /// <param name="id">The Relay global node ID string to parse.</param>
    /// <param name="typeName">
    /// When this method returns <c>true</c>, contains the type name extracted from the ID.
    /// When this method returns <c>false</c>, this parameter is <c>null</c>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the type name was successfully extracted from the ID;
    /// <c>false</c> if the ID format is invalid or the type name cannot be determined.
    /// </returns>
    /// <remarks>
    /// This method should be lightweight and focused solely on extracting routing information.
    /// It does not need to validate the complete ID structure or verify that the entity
    /// actually exists - those concerns are handled by the source schema that receives
    /// the routed request.
    /// </remarks>
    bool TryParseTypeName(string id, [NotNullWhen(true)] out string? typeName);
}
