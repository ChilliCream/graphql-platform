using System;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.Properties.TypeResources;

namespace HotChocolate.Types.Pagination;

/// <summary>
/// Represents an edge in a connection.
/// </summary>
internal sealed class EdgeType : ObjectType, IEdgeType
{
    internal EdgeType(
        NameString connectionName,
        ITypeReference nodeType)
    {
        if (nodeType is null)
        {
            throw new ArgumentNullException(nameof(nodeType));
        }

        ConnectionName = connectionName.EnsureNotEmpty(nameof(connectionName));
        Definition = CreateTypeDefinition(nodeType);
        Definition.Name = NameHelper.CreateEdgeName(connectionName);
        Definition.Configurations.Add(
            new CompleteConfiguration(
                (c, _) => NodeType = c.GetType<IOutputType>(nodeType),
                Definition,
                ApplyConfigurationOn.Completion));
    }

    internal EdgeType(ITypeReference nodeType)
    {
        if (nodeType is null)
        {
            throw new ArgumentNullException(nameof(nodeType));
        }

        Definition = CreateTypeDefinition(nodeType);
        Definition.Configurations.Add(
            new CompleteConfiguration(
                (c, d) =>
                {
                    IType type = c.GetType<IType>(nodeType);
                    ConnectionName = type.NamedType().Name;
                    ((ObjectTypeDefinition)d).Name = NameHelper.CreateEdgeName(ConnectionName);
                },
                Definition,
                ApplyConfigurationOn.Naming,
                nodeType,
                TypeDependencyKind.Named));
        Definition.Configurations.Add(
            new CompleteConfiguration(
                (c, _) => NodeType = c.GetType<IOutputType>(nodeType),
                Definition,
                ApplyConfigurationOn.Completion));
    }

    /// <summary>
    /// Gets the connection name of this connection type.
    /// </summary>
    public NameString ConnectionName { get; private set; }

    /// <inheritdoc />
    public IOutputType NodeType { get; private set; } = default!;

    /// <inheritdoc />
    [Obsolete("Use NodeType.")]
    public IOutputType EntityType => NodeType;

    /// <inheritdoc />
    public override bool IsInstanceOfType(IResolverContext context, object resolverResult)
    {
        if (resolverResult is IEdge { Node: not null } edge)
        {
            IType nodeType = NodeType;

            if (nodeType.Kind is TypeKind.NonNull)
            {
                nodeType = nodeType.InnerType();
            }

            if (nodeType.Kind is not TypeKind.Object)
            {
                throw new GraphQLException(
                    EdgeType_IsInstanceOfType_NonObject);
            }

            return ((ObjectType)nodeType).IsInstanceOfType(context, edge.Node);
        }

        return false;
    }

    private static ObjectTypeDefinition CreateTypeDefinition(ITypeReference nodeType)
        => new(default, EdgeType_Description, typeof(IEdge))
        {
            Fields =
            {
                    new(Names.Cursor,
                        EdgeType_Cursor_Description,
                        TypeReference.Parse($"{ScalarNames.String}!"),
                        pureResolver: GetCursor),
                    new(Names.Node,
                        EdgeType_Node_Description,
                        nodeType,
                        pureResolver: GetNode)
            }
        };

    private static string GetCursor(IPureResolverContext context)
        => context.Parent<IEdge>().Cursor;

    private static object? GetNode(IPureResolverContext context)
        => context.Parent<IEdge>().Node;

    private static class Names
    {
        public const string Cursor = "cursor";
        public const string Node = "node";
    }
}
