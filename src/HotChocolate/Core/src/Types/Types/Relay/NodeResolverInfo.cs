#nullable enable

using HotChocolate.Resolvers;

namespace HotChocolate.Types.Relay;

/// <summary>
/// The node resolver info is used by the fields node and nodes to execute 
/// the node resolver pipeline for a specific node.
/// </summary>
internal sealed class NodeResolverInfo
{
    public NodeResolverInfo(Argument? id, FieldDelegate pipeline)
    {
        Id = id;
        Pipeline = pipeline;
    }

    /// <summary>
    /// Gets the ID argument for a query field that doubles as node resolver.
    /// This property is null if the node resolver was not inferred from a query field.
    /// </summary>
    public Argument? Id { get; }

    /// <summary>
    /// Gets the node resolver pipeline.
    /// </summary>
    public FieldDelegate Pipeline { get; }
}
