using System.Diagnostics.CodeAnalysis;
using HotChocolate.Resolvers;

namespace HotChocolate.Types.Relay;

/// <summary>
/// The node resolver info is used by the fields node and nodes to execute
/// the node resolver pipeline for a specific node.
/// </summary>
internal sealed class NodeResolverInfo(
    ObjectField? resolverField,
    FieldDelegate pipeline)
{
    /// <summary>
    /// Gets the ID argument for a query field that doubles as node resolver.
    /// This property is null if the node resolver was not inferred from a query field.
    /// </summary>
    public Argument? Id { get; } = resolverField?.Arguments[0];

    /// <summary>
    /// Gets the node resolver pipeline.
    /// </summary>
    public FieldDelegate Pipeline { get; } = pipeline;

    /// <summary>
    /// Gets the query field from which we inferred the node resolver.
    /// </summary>
    public ObjectField? QueryField { get; } = resolverField;

    /// <summary>
    /// Defines if the node resolver was inferred from a query field.
    /// </summary>
    /// <value></value>
    [MemberNotNullWhen(true, nameof(QueryField), nameof(Id))]
    public bool IsQueryFieldResolver { get; } = resolverField is not null;
}
