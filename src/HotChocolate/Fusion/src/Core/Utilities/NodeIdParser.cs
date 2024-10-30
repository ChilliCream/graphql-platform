namespace HotChocolate.Fusion.Utilities;

/// <summary>
/// The ID parser is responsible for parsing node IDs and determining the type name of the node.
/// </summary>
public abstract class NodeIdParser
{
    /// <summary>
    /// Parses the type name from the node ID.
    /// </summary>
    /// <param name="id">
    /// The node ID.
    /// </param>
    /// <returns>
    /// Returns the type name of the node.
    /// </returns>
    public abstract string ParseTypeName(string id);
}
