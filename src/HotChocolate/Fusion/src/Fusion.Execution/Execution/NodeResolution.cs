namespace HotChocolate.Fusion.Execution;

/// <summary>
/// Determines how the gateway resolves the <c>Query.node</c> field, i.e. which participant is
/// responsible for turning a global object identifier into an object.
/// </summary>
public enum NodeResolution
{
    /// <summary>
    /// The gateway determines the object's type from the identifier and routes the lookup to the
    /// owning source schema. This is the default.
    /// </summary>
    Gateway,

    /// <summary>
    /// The gateway does not interpret the identifier and forwards <c>node(id:)</c> to a source
    /// schema that owns a node lookup, which resolves the object's type. Use this when identifiers
    /// are opaque to the gateway.
    /// </summary>
    SourceSchema
}
