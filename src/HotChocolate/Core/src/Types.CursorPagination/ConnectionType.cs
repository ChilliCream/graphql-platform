using HotChocolate.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;
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
        bool includeNodesField,
        INamingConventions namingConventions)
    {
        ArgumentException.ThrowIfNullOrEmpty(connectionName);
        ArgumentNullException.ThrowIfNull(nodeType);

        ConnectionName = connectionName;
        var edgeTypeName = NameHelper.CreateEdgeName(namingConventions, connectionName);

        var edgesType =
            TypeReference.Parse(
                $"[{edgeTypeName}!]",
                TypeContext.Output,
                factory: c => new EdgeType(connectionName, nodeType, c.Naming));

        Configuration = CreateConfiguration(includeTotalCount, includeNodesField, edgesType);
        Configuration.Name = NameHelper.CreateConnectionName(namingConventions, connectionName);
        Configuration.Dependencies.Add(new TypeDependency(nodeType));
        Configuration.Tasks.Add(
            new OnCompleteTypeSystemConfigurationTask(
                (c, _) => EdgeType = c.GetType<IEdgeType>(TypeReference.Create(edgeTypeName)),
                Configuration,
                ApplyConfigurationOn.BeforeCompletion));

        if (includeNodesField)
        {
            Configuration.Tasks.Add(
                new OnCompleteTypeSystemConfigurationTask(
                    (c, d) =>
                    {
                        var definition = (ObjectTypeConfiguration)d;
                        var nodes = definition.Fields.First(IsNodesField);
                        nodes.Type = TypeReference.Parse(
                            $"[{c.GetType<IType>(nodeType).Print()}]",
                            TypeContext.Output);
                    },
                    Configuration,
                    ApplyConfigurationOn.BeforeNaming,
                    nodeType,
                    TypeDependencyFulfilled.Named));
        }
    }

    internal ConnectionType(TypeReference nodeType, bool includeTotalCount, bool includeNodesField)
    {
        ArgumentNullException.ThrowIfNull(nodeType);

        var edgeType =
            TypeReference.Create(
                ContextDataKeys.EdgeType,
                nodeType,
                c => new EdgeType(nodeType, c.Naming),
                TypeContext.Output);

        // the property is set later in the configuration
        ConnectionName = null!;
        Configuration = CreateConfiguration(includeTotalCount, includeNodesField);
        Configuration.Dependencies.Add(new TypeDependency(nodeType));
        Configuration.Dependencies.Add(new TypeDependency(edgeType));
        Configuration.NeedsNameCompletion = true;

        Configuration.Tasks.Add(
            new OnCompleteTypeSystemConfigurationTask(
                (c, d) =>
                {
                    var type = c.GetType<IType>(nodeType);
                    ConnectionName = type.NamedType().Name;

                    var definition = (ObjectTypeConfiguration)d;
                    var edges = definition.Fields.First(IsEdgesField);

                    definition.Name = NameHelper.CreateConnectionName(c.DescriptorContext.Naming, ConnectionName);
                    edges.Type = TypeReference.Parse(
                        $"[{NameHelper.CreateEdgeName(c.DescriptorContext.Naming, ConnectionName)}!]",
                        TypeContext.Output);

                    if (includeNodesField)
                    {
                        var nodes = definition.Fields.First(IsNodesField);
                        nodes.Type = TypeReference.Parse(
                            $"[{type.Print()}]",
                            TypeContext.Output);
                    }
                },
                Configuration,
                ApplyConfigurationOn.BeforeNaming,
                nodeType,
                TypeDependencyFulfilled.Named));
        Configuration.Tasks.Add(
            new OnCompleteTypeSystemConfigurationTask(
                (c, _) => EdgeType = c.GetType<IEdgeType>(edgeType),
                Configuration,
                ApplyConfigurationOn.BeforeCompletion));
    }

    /// <summary>
    /// Gets the connection name of this connection type.
    /// </summary>
    public string ConnectionName { get; private set; }

    /// <summary>
    /// Gets the edge type of this connection.
    /// </summary>
    public IEdgeType EdgeType { get; private set; } = null!;

    IOutputType IPageType.ItemType => EdgeType;

    protected override void OnBeforeRegisterDependencies(
        ITypeDiscoveryContext context,
        TypeSystemConfiguration configuration)
    {
        context.Dependencies.Add(new TypeDependency(
            context.TypeInspector.GetOutputTypeRef(typeof(PageInfoType))));

        base.OnBeforeRegisterDependencies(context, configuration);
    }

    protected override void OnBeforeCompleteType(ITypeCompletionContext context, TypeSystemConfiguration configuration)
    {
        Configuration!.IsOfType = IsOfTypeWithRuntimeType;
        base.OnBeforeCompleteType(context, configuration);
    }

    private bool IsOfTypeWithRuntimeType(
        IResolverContext context,
        object? result) =>
        result is null || RuntimeType.IsInstanceOfType(result);

    private static ObjectTypeConfiguration CreateConfiguration(
        bool includeTotalCount,
        bool includeNodesField,
        TypeReference? edgesType = null)
    {
        var definition = new ObjectTypeConfiguration
        {
            Description = ConnectionType_Description,
            RuntimeType = typeof(Connection)
        };

        definition.Fields.Add(
            new ObjectFieldConfiguration(
                Names.PageInfo,
                ConnectionType_PageInfo_Description,
                TypeReference.Parse("PageInfo!"),
                pureResolver: GetPagingInfo));

        definition.Fields.Add(
            new ObjectFieldConfiguration(
                Names.Edges,
                ConnectionType_Edges_Description,
                edgesType,
                pureResolver: GetEdges)
            {
                Flags = CoreFieldFlags.ConnectionEdgesField
            });

        if (includeNodesField)
        {
            definition.Fields.Add(
                new ObjectFieldConfiguration(
                    Names.Nodes,
                    ConnectionType_Nodes_Description,
                    pureResolver: GetNodes)
                {
                    Flags = CoreFieldFlags.ConnectionNodesField
                });
        }

        if (includeTotalCount)
        {
            definition.Fields.Add(
                new ObjectFieldConfiguration(
                    Names.TotalCount,
                    ConnectionType_TotalCount_Description,
                    type: TypeReference.Parse($"{ScalarNames.Int}!"),
                    pureResolver: GetTotalCount)
                {
                    Flags = CoreFieldFlags.TotalCount
                });
        }

        return definition;
    }

    private static bool IsEdgesField(ObjectFieldConfiguration field)
        => (field.Flags & CoreFieldFlags.ConnectionEdgesField) == CoreFieldFlags.ConnectionEdgesField;

    private static bool IsNodesField(ObjectFieldConfiguration field)
        => (field.Flags & CoreFieldFlags.ConnectionNodesField) == CoreFieldFlags.ConnectionNodesField;

    private static IPageInfo GetPagingInfo(IResolverContext context)
        => context.Parent<IConnection>().Info;

    private static IEnumerable<IEdge>? GetEdges(IResolverContext context)
        => context.Parent<IConnection>().Edges;

    private static IEnumerable<object?>? GetNodes(IResolverContext context)
        => context.Parent<IConnection>().Edges?.Select(t => t.Node);

    private static object? GetTotalCount(IResolverContext context)
        => context.Parent<IPageTotalCountProvider>().TotalCount;

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
