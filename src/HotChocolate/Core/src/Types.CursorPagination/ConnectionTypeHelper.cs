using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Pagination;

/// <summary>
/// Provides helpers to create type references for connection types.
/// The helpers are used by source-generated type initialization code.
/// </summary>
public static class ConnectionTypeHelper
{
    /// <summary>
    /// Creates a type reference for a field that returns
    /// <see cref="Connection{T}"/> or <see cref="IConnection{T}"/>.
    /// The referenced connection type exposes the standard connection fields
    /// (edges, nodes, pageInfo and totalCount) for the specified node type.
    /// </summary>
    /// <param name="context">
    /// The descriptor context.
    /// </param>
    /// <param name="connectionName">
    /// The connection name, for example <c>Book</c> for a <c>BookConnection</c>.
    /// </param>
    /// <param name="nodeType">
    /// The type reference of the node type.
    /// </param>
    /// <param name="nonNull">
    /// Specifies if the connection type is wrapped in a non-null type.
    /// </param>
    /// <returns>
    /// A type reference that can be set as the field type.
    /// </returns>
    public static TypeReference CreateConnectionTypeReference(
        IDescriptorContext context,
        string connectionName,
        TypeReference nodeType,
        bool nonNull)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrEmpty(connectionName);
        ArgumentNullException.ThrowIfNull(nodeType);

        var typeName = NameHelper.CreateConnectionName(context.Naming, connectionName);

        return TypeReference.Parse(
            nonNull ? $"{typeName}!" : typeName,
            TypeContext.Output,
            factory: c => new ConnectionType(
                connectionName,
                nodeType,
                includeTotalCount: true,
                includeNodesField: true,
                c.Naming));
    }
}
