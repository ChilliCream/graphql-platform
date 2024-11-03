#nullable enable

using System.Diagnostics.CodeAnalysis;
using HotChocolate.Resolvers;

namespace HotChocolate.Types.Relay;

/// <summary>
/// The node resolver info is used by the fields node and nodes to execute
/// the node resolver pipeline for a specific node.
/// </summary>
internal sealed class NodeResolverInfo
{
    public NodeResolverInfo(ObjectField? resolverField, FieldDelegate pipeline)
    {
        Id = resolverField?.Arguments[0];
        Pipeline = pipeline;
        QueryField = resolverField;
        IsQueryFieldResolver = resolverField is not null;
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

    /// <summary>
    /// Gets the query field from which we inferred the node resolver.
    /// </summary>
    public ObjectField? QueryField { get; }

    /// <summary>
    /// Defines if the node resolver was inferred from a query field.
    /// </summary>
    /// <value></value>
    [MemberNotNullWhen(true, nameof(QueryField), nameof(Id))]
    public bool IsQueryFieldResolver { get; }
}
