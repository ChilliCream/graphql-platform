using HotChocolate.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.Properties.TypeResources;

namespace HotChocolate.Types.Pagination;

/// <summary>
/// The connection type.
/// </summary>
internal sealed class ConnectionType
    : ObjectType
    , IConnectionType
    , IPageType
{
    internal ConnectionType(
        string connectionName,
        TypeReference nodeType,
        bool includeTotalCount,
        bool includeNodesField)
    {
        if (nodeType is null)
        {
            throw new ArgumentNullException(nameof(nodeType));
        }

        if (string.IsNullOrEmpty(connectionName))
        {
            throw new ArgumentNullException(nameof(connectionName));
        }

        ConnectionName = connectionName;
        var edgeTypeName = NameHelper.CreateEdgeName(connectionName);

        var edgesType =
            TypeReference.Parse(
                $"[{edgeTypeName}!]",
                TypeContext.Output,
                factory: _ => new EdgeType(connectionName, nodeType));

        Definition = CreateTypeDefinition(includeTotalCount, includeNodesField, edgesType);
        Definition.Name = NameHelper.CreateConnectionName(connectionName);
        Definition.Dependencies.Add(new(nodeType));
        Definition.Configurations.Add(
            new CompleteConfiguration(
                (c, _) => EdgeType = c.GetType<IEdgeType>(TypeReference.Create(edgeTypeName)),
                Definition,
                ApplyConfigurationOn.BeforeCompletion));

        if (includeNodesField)
        {
            Definition.Configurations.Add(
                new CompleteConfiguration(
                    (c, d) =>
                    {
                        var definition = (ObjectTypeDefinition)d;
                        var nodes = definition.Fields.First(IsNodesField);
                        nodes.Type = TypeReference.Parse(
                            $"[{c.GetType<IType>(nodeType).Print()}]",
                            TypeContext.Output);
                    },
                    Definition,
                    ApplyConfigurationOn.BeforeNaming,
                    nodeType,
                    TypeDependencyFulfilled.Named));
        }
    }

    internal ConnectionType(TypeReference nodeType, bool includeTotalCount, bool includeNodesField)
    {
        if (nodeType is null)
        {
            throw new ArgumentNullException(nameof(nodeType));
        }

        var edgeType =
            TypeReference.Create(
                ContextDataKeys.EdgeType,
                nodeType,
                _ => new EdgeType(nodeType),
                TypeContext.Output);

        // the property is set later in the configuration
        ConnectionName = default!;
        Definition = CreateTypeDefinition(includeTotalCount, includeNodesField);
        Definition.Dependencies.Add(new(nodeType));
        Definition.Dependencies.Add(new(edgeType));
        Definition.NeedsNameCompletion = true;

        Definition.Configurations.Add(
            new CompleteConfiguration(
                (c, d) =>
                {
                    var type = c.GetType<IType>(nodeType);
                    ConnectionName = type.NamedType().Name;

                    var definition = (ObjectTypeDefinition)d;
                    var edges = definition.Fields.First(IsEdgesField);

                    definition.Name = NameHelper.CreateConnectionName(ConnectionName);
                    edges.Type = TypeReference.Parse(
                        $"[{NameHelper.CreateEdgeName(ConnectionName)}!]",
                        TypeContext.Output);

                    if (includeNodesField)
                    {
                        var nodes = definition.Fields.First(IsNodesField);
                        nodes.Type = TypeReference.Parse(
                            $"[{type.Print()}]",
                            TypeContext.Output);
                    }
                },
                Definition,
                ApplyConfigurationOn.BeforeNaming,
                nodeType,
                TypeDependencyFulfilled.Named));
        Definition.Configurations.Add(
            new CompleteConfiguration(
                (c, _) =>
                {
                    EdgeType = c.GetType<IEdgeType>(edgeType);
                },
                Definition,
                ApplyConfigurationOn.BeforeCompletion));
    }

    /// <summary>
    /// Gets the connection name of this connection type.
    /// </summary>
    public string ConnectionName { get; private set; }

    /// <summary>
    /// Gets the edge type of this connection.
    /// </summary>
    public IEdgeType EdgeType { get; private set; } = default!;

    IOutputType IPageType.ItemType => EdgeType;

    protected override void OnBeforeRegisterDependencies(
        ITypeDiscoveryContext context,
        DefinitionBase definition)
    {
        context.Dependencies.Add(new(
            context.TypeInspector.GetOutputTypeRef(typeof(PageInfoType))));

        base.OnBeforeRegisterDependencies(context, definition);
    }

    protected override void OnBeforeCompleteType(ITypeCompletionContext context, DefinitionBase definition)
    {
        Definition!.IsOfType = IsOfTypeWithRuntimeType;
        base.OnBeforeCompleteType(context, definition);
    }

    private bool IsOfTypeWithRuntimeType(
        IResolverContext context,
        object? result) =>
        result is null || RuntimeType.IsInstanceOfType(result);

    private static ObjectTypeDefinition CreateTypeDefinition(
        bool includeTotalCount,
        bool includeNodesField,
        TypeReference? edgesType = null)
    {
        var definition = new ObjectTypeDefinition
        {
            Description = ConnectionType_Description,
            RuntimeType = typeof(Connection)
        };

        definition.Fields.Add(new(
            Names.PageInfo,
            ConnectionType_PageInfo_Description,
            TypeReference.Parse("PageInfo!"),
            pureResolver: GetPagingInfo));

        definition.Fields.Add(new(
            Names.Edges,
            ConnectionType_Edges_Description,
            edgesType,
            pureResolver: GetEdges)
        { Flags = FieldFlags.ConnectionEdgesField });
        if (includeNodesField)
        {
            definition.Fields.Add(new(
                Names.Nodes,
                ConnectionType_Nodes_Description,
                pureResolver: GetNodes)
                { Flags = FieldFlags.ConnectionNodesField });
        }

        if (includeTotalCount)
        {
            definition.Fields.Add(new(
                Names.TotalCount,
                ConnectionType_TotalCount_Description,
                type: TypeReference.Parse($"{ScalarNames.Int}!"),
                pureResolver: GetTotalCount)
            {
                Flags = FieldFlags.TotalCount
            });
        }

        return definition;
    }

    private static bool IsEdgesField(ObjectFieldDefinition field)
        => (field.Flags & FieldFlags.ConnectionEdgesField) == FieldFlags.ConnectionEdgesField;

    private static bool IsNodesField(ObjectFieldDefinition field)
        => (field.Flags & FieldFlags.ConnectionNodesField) == FieldFlags.ConnectionNodesField;

    private static IPageInfo GetPagingInfo(IResolverContext context)
        => context.Parent<Connection>().Info;

    private static IReadOnlyCollection<IEdge> GetEdges(IResolverContext context)
        => context.Parent<Connection>().Edges;

    private static IEnumerable<object?> GetNodes(IResolverContext context)
        => context.Parent<Connection>().Edges.Select(t => t.Node);

    private static object? GetTotalCount(IResolverContext context)
        => context.Parent<Connection>().TotalCount;

    internal static class Names
    {
        public const string PageInfo = "pageInfo";
        public const string Edges = "edges";
        public const string Nodes = "nodes";
        public const string TotalCount = "totalCount";
    }

    private static class ContextDataKeys
    {
        public const string EdgeType = "HotChocolate_Types_Edge";
    }
}
