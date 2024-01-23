#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.Properties.TypeResources;

namespace HotChocolate.Types.Pagination;

/// <summary>
/// The connection type.
/// </summary>
internal class ConnectionType
    : ObjectType
    , IConnectionType
    , IPageType
{
    internal ConnectionType(
        string connectionName,
        TypeReference nodeType,
        bool withTotalCount)
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

        Definition = CreateTypeDefinition(withTotalCount, edgesType);
        Definition.Name = NameHelper.CreateConnectionName(connectionName);
        Definition.Dependencies.Add(new(nodeType));
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
        Definition.Configurations.Add(
            new CompleteConfiguration(
                (c, _) => EdgeType = c.GetType<IEdgeType>(TypeReference.Create(edgeTypeName)),
                Definition,
                ApplyConfigurationOn.BeforeCompletion));
    }

    internal ConnectionType(TypeReference nodeType, bool withTotalCount)
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
        Definition = CreateTypeDefinition(withTotalCount);
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
                    var nodes = definition.Fields.First(IsNodesField);

                    definition.Name = NameHelper.CreateConnectionName(ConnectionName);
                    edges.Type = TypeReference.Parse(
                        $"[{NameHelper.CreateEdgeName(ConnectionName)}!]",
                        TypeContext.Output);

                    nodes.Type = TypeReference.Parse(
                        $"[{type.Print()}]",
                        TypeContext.Output);
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

    private static ObjectTypeDefinition CreateTypeDefinition(
        bool withTotalCount,
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
        { CustomSettings = { ContextDataKeys.Edges, }, });

        definition.Fields.Add(new(
            Names.Nodes,
            ConnectionType_Nodes_Description,
            pureResolver: GetNodes)
        { CustomSettings = { ContextDataKeys.Nodes, }, });

        if (withTotalCount)
        {
            definition.Fields.Add(new(
                Names.TotalCount,
                ConnectionType_TotalCount_Description,
                type: TypeReference.Parse($"{ScalarNames.Int}!"),
                resolver: GetTotalCountAsync));
        }

        return definition;
    }

    private static bool IsEdgesField(ObjectFieldDefinition field)
        => field.CustomSettings.Count > 0 &&
           field.CustomSettings[0].Equals(ContextDataKeys.Edges);

    private static bool IsNodesField(ObjectFieldDefinition field)
        => field.CustomSettings.Count > 0 &&
           field.CustomSettings[0].Equals(ContextDataKeys.Nodes);

    private static IPageInfo GetPagingInfo(IPureResolverContext context)
        => context.Parent<Connection>().Info;

    private static IReadOnlyCollection<IEdge> GetEdges(IPureResolverContext context)
        => context.Parent<Connection>().Edges;

    private static IEnumerable<object?> GetNodes(IPureResolverContext context)
        => context.Parent<Connection>().Edges.Select(t => t.Node);

    private static async ValueTask<object?> GetTotalCountAsync(IResolverContext context)
        => await context.Parent<Connection>().GetTotalCountAsync(context.RequestAborted);

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
        public const string Edges = "HotChocolate.Types.Connection.Edges";
        public const string Nodes = "HotChocolate.Types.Connection.Nodes";
    }
}
