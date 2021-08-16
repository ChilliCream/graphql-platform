using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Pagination
{
    public class ConnectionType<T>
        : ObjectType<Connection>
        , IPageType
        where T : class, IOutputType
    {
        public ConnectionType()
        {
        }

        public ConnectionType(Action<IObjectTypeDescriptor<Connection>> configure)
            : base(descriptor =>
            {
                ApplyConfig(descriptor);
                configure(descriptor);
            })
        {
        }

        public IEdgeType EdgeType { get; private set; } = default!;

        IOutputType IPageType.ItemType => EdgeType;

        protected override void Configure(IObjectTypeDescriptor<Connection> descriptor) =>
            ApplyConfig(descriptor);

        protected static void ApplyConfig(IObjectTypeDescriptor<Connection> descriptor)
        {
            descriptor
                .Name(dependency => $"{dependency.Name}Connection")
                .DependsOn<T>()
                .Description("A connection to a list of items.")
                .BindFields(BindingBehavior.Explicit);

            descriptor
                .Field(t => t.Info)
                .Name("pageInfo")
                .Description("Information to aid in pagination.")
                .Type<NonNullType<PageInfoType>>();

            descriptor
                .Field(t => t.Edges)
                .Name("edges")
                .Description("A list of edges.")
                .Type<ListType<NonNullType<EdgeType<T>>>>();

            descriptor
                .Field(t => t.Edges.Select(t => t.Node))
                .Name("nodes")
                .Description("A flattened list of the nodes.")
                .Type<ListType<T>>()
                .Resolve(ctx => ctx.Parent<Connection>().Edges.Select(t => t.Node))
                .Extend()
                .OnBeforeCreate(
                    d => d.PureResolver =
                        ctx => ctx.Parent<Connection>().Edges.Select(t => t.Node));
        }

        protected override void OnRegisterDependencies(
            ITypeDiscoveryContext context,
            ObjectTypeDefinition definition)
        {
            base.OnRegisterDependencies(context, definition);

            context.RegisterDependency(
                context.TypeInspector.GetTypeRef(typeof(EdgeType<T>)),
                TypeDependencyKind.Default);
        }

        protected override void OnCompleteType(
            ITypeCompletionContext context,
            ObjectTypeDefinition definition)
        {
            base.OnCompleteType(context, definition);

            EdgeType = context.GetType<EdgeType<T>>(
                context.TypeInspector.GetTypeRef(typeof(EdgeType<T>)));
        }
    }

    public class ConnectionType : ObjectType, IPageType
    {
        private const string _edgesField = "HotChocolate.Types.Connection.Edges";
        private const string _nodesField = "HotChocolate.Types.Connection.Nodes";

        internal ConnectionType(
            NameString connectionName,
            ITypeReference nodeType,
            bool withTotalCount)
        {
            if (nodeType is null)
            {
                throw new ArgumentNullException(nameof(nodeType));
            }

            ConnectionName = connectionName.EnsureNotEmpty(nameof(connectionName));

            SyntaxTypeReference edgeType =
                TypeReference.Parse(
                    $"[{NameHelper.CreateEdgeName(ConnectionName)}!]",
                    TypeContext.Output,
                    factory: _ => new EdgeType(ConnectionName, nodeType));

            Definition = CreateTypeDefinition(withTotalCount, edgeType);
            Definition.Dependencies.Add(new(nodeType));
            Definition.Configurations.Add(
                new CompleteConfiguration(
                    (c, d) =>
                    {
                        var definition = (ObjectTypeDefinition)d;
                        ObjectFieldDefinition nodes = definition.Fields.First(IsNodesField);
                        nodes.Type = TypeReference.Parse(
                            $"[{c.GetType<IType>(nodeType).Print()}]",
                            TypeContext.Output);
                    },
                    Definition,
                    ApplyConfigurationOn.Naming,
                    nodeType,
                    TypeDependencyKind.Named));
        }

        internal ConnectionType(ITypeReference nodeType, bool withTotalCount)
        {
            if (nodeType is null)
            {
                throw new ArgumentNullException(nameof(nodeType));
            }

            SyntaxTypeReference edgeType =
                TypeReference.Parse(
                    $"Temp_{Guid.NewGuid():N}",
                    TypeContext.Output,
                    factory: _ => new EdgeType(nodeType));

            Definition = CreateTypeDefinition(withTotalCount);
            Definition.Dependencies.Add(new(nodeType));
            Definition.NeedsNameCompletion = true;

            Definition.Configurations.Add(
                new CompleteConfiguration(
                    (c, d) =>
                    {
                        IType type = c.GetType<IType>(nodeType);
                        ConnectionName = type.NamedType().Name;

                        var definition = (ObjectTypeDefinition)d;
                        ObjectFieldDefinition edges = definition.Fields.First(IsEdgesField);
                        ObjectFieldDefinition nodes = definition.Fields.First(IsNodesField);

                        edges.Type = TypeReference.Parse(
                            $"[{NameHelper.CreateEdgeName(ConnectionName)}!]",
                            TypeContext.Output);;
                        nodes.Type = TypeReference.Parse(
                            $"[{type.Print()}]",
                            TypeContext.Output);
                    },
                    Definition,
                    ApplyConfigurationOn.Naming,
                    nodeType,
                    TypeDependencyKind.Named));
        }

        /// <summary>
        /// Gets the connection name of this connection type.
        /// </summary>
        public NameString ConnectionName { get; private set; } = default!;

        public IEdgeType EdgeType { get; private set; } = default!;

        IOutputType IPageType.ItemType => EdgeType;

        protected override void OnBeforeRegisterDependencies(
            ITypeDiscoveryContext context,
            DefinitionBase definition,
            IDictionary<string, object?> contextData)
        {
            context.RegisterDependency(
                context.TypeInspector.GetOutputTypeRef(typeof(PageInfoType)),
                TypeDependencyKind.Default);

            base.OnBeforeRegisterDependencies(context, definition, contextData);
        }

        protected override void OnCompleteType(
            ITypeCompletionContext context,
            ObjectTypeDefinition definition)
        {
            EdgeType = context.GetType<IEdgeType>(
                TypeReference.Create(
                    NameHelper.CreateEdgeName(ConnectionName),
                    TypeContext.Output));

            ObjectFieldDefinition edges = definition.Fields.First(IsEdgesField);

            ObjectFieldDefinition nodes = definition.Fields.First(IsNodesField);



            base.OnCompleteType(context, definition);
        }

        private NameString CreateConnectionNameFromNodeType(
            ITypeCompletionContext context,
            ITypeReference nodeType)
            => context.GetType<IType>(nodeType).NamedType().Name;

        private static ObjectTypeDefinition CreateTypeDefinition(
            bool withTotalCount,
            ITypeReference? edgesType = null)
        {
            var definition = new ObjectTypeDefinition(
                default,
                "A connection to a list of items.",
                typeof(Connection));

            definition.Fields.Add(new(
                "pageInfo",
                "Information to aid in pagination.",
                TypeReference.Parse("PageInfo!"),
                pureResolver: GetPagingInfo));

            definition.Fields.Add(new(
                "edges",
                "A list of edges.",
                edgesType,
                pureResolver: GetEdges)
            { CustomSettings = { _edgesField } });

            definition.Fields.Add(new(
                "nodes",
                "A flattened list of the nodes.",
                pureResolver: GetNodes)
            { CustomSettings = { _nodesField } });

            if (withTotalCount)
            {
                definition.Fields.Add(new(
                    "totalCount",
                    type: TypeReference.Parse("Int!"),
                    resolver: GetTotalCountAsync));
            }

            return definition;
        }

        private static bool IsEdgesField(ObjectFieldDefinition field)
            => field.CustomSettings.Count > 0 && field.CustomSettings[0].Equals(_edgesField);

        private static bool IsNodesField(ObjectFieldDefinition field)
            => field.CustomSettings.Count > 0 && field.CustomSettings[0].Equals(_nodesField);

        private static IPageInfo GetPagingInfo(IPureResolverContext context)
            => context.Parent<Connection>().Info;

        private static IReadOnlyCollection<IEdge> GetEdges(IPureResolverContext context)
            => context.Parent<Connection>().Edges;

        private static IEnumerable<object?> GetNodes(IPureResolverContext context)
            => context.Parent<Connection>().Edges.Select(t => t.Node);

        private static async ValueTask<object?> GetTotalCountAsync(IResolverContext context)
            => await context.Parent<Connection>().GetTotalCountAsync(context.RequestAborted);
    }

    internal static class NameHelper
    {
        public static string CreateConnectionName(NameString connectionName)
            => connectionName + "Connection";

        public static string CreateEdgeName(NameString connectionName)
            => connectionName + "Edge";
    }
}
