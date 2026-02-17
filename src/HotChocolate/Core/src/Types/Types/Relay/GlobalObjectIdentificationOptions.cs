namespace HotChocolate.Types.Relay;

/// <summary>
/// Configures global object identification behavior according to the Relay specification.
/// Global object identification enables clients to refetch any object using a globally unique ID
/// through the standardized <c>node</c> and <c>nodes</c> fields on the Query type.
/// </summary>
public sealed class GlobalObjectIdentificationOptions
{
    /// <summary>
    /// Gets or sets whether the Node interface and the `Query.node` field are registered with the schema.
    /// When enabled, adds the Node interface and a <c>node(id: ID!): Node</c> field to Query.
    /// </summary>
    /// <value>
    /// <c>true</c> to register the Node interface and node field; otherwise, <c>false</c>.
    /// Default is <c>true</c>.
    /// </value>
    public bool RegisterNodeInterface { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to validate that all Node-implementing types can be resolved through the node field.
    /// When enabled, the schema builder verifies that every type implementing Node has a corresponding
    /// node resolver configured, preventing runtime errors from unresolvable node IDs.
    /// </summary>
    /// <value>
    /// <c>true</c> to validate node resolvability during schema building; otherwise, <c>false</c>.
    /// Default is <c>true</c>.
    /// </value>
    public bool EnsureAllNodesCanBeResolved { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of nodes that can be fetched in a single request to the nodes field.
    /// This limit prevents excessive resource usage when clients request large batches of objects.
    /// Requests exceeding this limit will result in a validation error.
    /// </summary>
    /// <value>
    /// The maximum batch size for the nodes field. Default is 50.
    /// </value>
    public int MaxAllowedNodeBatchSize { get; set; } = 50;

    /// <summary>
    /// Gets or sets whether a plural <c>nodes(ids: [ID!]!): [Node]!</c> field is added to the Query type.
    /// The nodes field allows clients to efficiently fetch multiple objects by their global IDs
    /// in a single request, which is useful for client-side caching and batching scenarios.
    /// </summary>
    /// <value>
    /// <c>true</c> to add the nodes field to Query; otherwise, <c>false</c>.
    /// Default is <c>true</c>.
    /// </value>
    public bool AddNodesField { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the <c>Query.node</c> field should be annotated with the <c>@lookup</c> directive
    /// for composite schema spec. This directive marks the field as an entity resolution point,
    /// enabling the Fusion gateway to resolve entities across source schemas using global object identification.
    /// </summary>
    /// <value>
    /// <c>true</c> to annotate the node field with <c>@lookup</c>; otherwise, <c>false</c>.
    /// Default is <c>false</c>.
    /// </value>
    /// <remarks>
    /// Enable this option when using Hot Chocolate Fusion to ensure proper entity resolution
    /// across source schemas. The <c>@lookup</c> directive tells the gateway that this field
    /// can be used to resolve entities by their global ID.
    /// </remarks>
    public bool MarkNodeFieldAsLookup { get; set; } = true;
}
