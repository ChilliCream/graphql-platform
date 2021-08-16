using System;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Pagination
{
    public class EdgeType<T>
        : ObjectType<IEdge>
        , IEdgeType
        where T : class, IOutputType
    {
        public IOutputType NodeType { get; private set; } = default!;

        [Obsolete("Use NodeType.")]
        public IOutputType EntityType => NodeType;

        protected override void Configure(
            IObjectTypeDescriptor<IEdge> descriptor)
        {
            descriptor
                .Name(dependency => dependency.Name + "Edge")
                .DependsOn<T>()
                .Description("An edge in a connection.")
                .BindFields(BindingBehavior.Explicit);

            descriptor
                .Field(t => t.Cursor)
                .Name("cursor")
                .Description("A cursor for use in pagination.")
                .Type<NonNullType<StringType>>();

            descriptor
                .Field(t => t.Node)
                .Name("node")
                .Description("The item at the end of the edge.")
                .Type<T>();
        }

        protected override void OnCompleteType(
            ITypeCompletionContext context,
            ObjectTypeDefinition definition)
        {
            base.OnCompleteType(context, definition);

            NodeType = context.GetType<IOutputType>(
                context.TypeInspector.GetTypeRef(typeof(T)));
        }
    }

    internal sealed class EdgeType : ObjectType, IEdgeType
    {
        private static readonly string _nodeTag = Guid.NewGuid().ToString("N");

        internal EdgeType(
            NameString connectionName,
            ITypeReference nodeType)
        {
            if (nodeType is null)
            {
                throw new ArgumentNullException(nameof(nodeType));
            }

            ConnectionName = connectionName.EnsureNotEmpty(nameof(connectionName));

            Definition = new(
                connectionName + "Edge",
                "An edge in a connection.",
                typeof(IEdge));

            Definition.Fields.Add(new(
                "cursor",
                "A cursor for use in pagination.",
                TypeReference.Parse("String!"),
                pureResolver: GetCursor));

            Definition.Fields.Add(new(
                "node",
                "The item at the end of the edge.",
                nodeType,
                pureResolver: GetNode)
            {
                CustomSettings = { _nodeTag }
            });
        }

        internal EdgeType(ITypeReference nodeType)
        {
        }

        /// <summary>
        /// Gets the connection name of this connection type.
        /// </summary>
        public NameString ConnectionName { get; }

        /// <inheritdoc />
        public IOutputType NodeType { get; private set; } = default!;

        /// <inheritdoc />
        [Obsolete("Use NodeType.")]
        public IOutputType EntityType => NodeType;

        protected override void OnCompleteType(
            ITypeCompletionContext context,
            ObjectTypeDefinition definition)
        {
            base.OnCompleteType(context, definition);

            NodeType = context.GetType<IOutputType>(
                definition.Fields.First(IsNodeField).Type!);
        }

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
                    // TODO : RESOURCES
                    throw new GraphQLException(
                        "Edge types that have a non object node are not supported.");
                }

                return ((ObjectType)nodeType).IsInstanceOfType(context, edge.Node);
            }

            return false;
        }

        private static string? GetCursor(IPureResolverContext context)
            => context.Parent<IEdge>().Cursor;

        private static object? GetNode(IPureResolverContext context)
            => context.Parent<IEdge>().Node;

        private static bool IsNodeField(ObjectFieldDefinition definition)
        {
            var customSettings = definition.GetCustomSettings();

            if (customSettings.Count == 0)
            {
                return false;
            }

            foreach (object obj in customSettings)
            {
                if (ReferenceEquals(obj, _nodeTag))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
