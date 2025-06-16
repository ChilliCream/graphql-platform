using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;
using static HotChocolate.Properties.TypeResources;

namespace HotChocolate.Types.Pagination;

/// <summary>
/// Represents an edge in a connection.
/// </summary>
internal sealed class EdgeType : ObjectType, IEdgeType
{
    internal EdgeType(
        string connectionName,
        TypeReference nodeType)
    {
        ArgumentException.ThrowIfNullOrEmpty(connectionName);
        ArgumentNullException.ThrowIfNull(nodeType);

        ConnectionName = connectionName;
        Configuration = CreateConfiguration(nodeType);
        Configuration.Name = NameHelper.CreateEdgeName(connectionName);
        Configuration.Tasks.Add(
            new OnCompleteTypeSystemConfigurationTask(
                (c, _) => NodeType = c.GetType<IOutputType>(nodeType),
                Configuration,
                ApplyConfigurationOn.BeforeCompletion));
    }

    internal EdgeType(TypeReference nodeType)
    {
        ArgumentNullException.ThrowIfNull(nodeType);

        // the property is set later in the configuration.
        ConnectionName = null!;
        Configuration = CreateConfiguration(nodeType);
        Configuration.Tasks.Add(
            new OnCompleteTypeSystemConfigurationTask(
                (c, d) =>
                {
                    var type = c.GetType<IType>(nodeType);
                    ConnectionName = type.NamedType().Name;
                    ((ObjectTypeConfiguration)d).Name = NameHelper.CreateEdgeName(ConnectionName);
                },
                Configuration,
                ApplyConfigurationOn.BeforeNaming,
                nodeType,
                TypeDependencyFulfilled.Named));
        Configuration.Tasks.Add(
            new OnCompleteTypeSystemConfigurationTask(
                (c, _) => NodeType = c.GetType<IOutputType>(nodeType),
                Configuration,
                ApplyConfigurationOn.BeforeCompletion));
    }

    /// <summary>
    /// Gets the connection name of this connection type.
    /// </summary>
    public string ConnectionName { get; private set; }

    /// <inheritdoc />
    public IOutputType NodeType { get; private set; } = null!;

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

    private static ObjectTypeConfiguration CreateConfiguration(TypeReference nodeType)
        => new()
        {
            Description = EdgeType_Description,
            RuntimeType = typeof(IEdge),
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

    private static string GetCursor(IResolverContext context)
        => context.Parent<IEdge>().Cursor;

    private static object? GetNode(IResolverContext context)
        => context.Parent<IEdge>().Node;

    private static class Names
    {
        public const string Cursor = "cursor";
        public const string Node = "node";
    }
}
