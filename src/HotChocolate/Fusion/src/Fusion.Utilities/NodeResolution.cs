namespace HotChocolate.Fusion;

/// <summary>
/// Determines how the router resolves the <c>Query.node</c> field, i.e. which participant is
/// responsible for turning a global object identifier into an object.
/// </summary>
public enum NodeResolution
{
    /// <summary>
    /// The router determines the object's type from the identifier and routes the lookup to the
    /// owning source schema. This is the default.
    /// </summary>
    Router = 0,

    /// <summary>
    /// The router determines the object's type from the identifier and routes the lookup to the
    /// owning source schema. This is the default.
    /// </summary>
    // TODO [17]: Remove the Gateway alias once all consumers have migrated to Router.
    [Obsolete("Use NodeResolution.Router instead.")]
    Gateway = Router,

    /// <summary>
    /// The router does not interpret the identifier and forwards <c>node(id:)</c> to a source
    /// schema that owns a node lookup, which resolves the object's type. Use this when identifiers
    /// are opaque to the router.
    /// </summary>
    SourceSchema = 1
}
