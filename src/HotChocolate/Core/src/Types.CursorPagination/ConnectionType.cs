using System;
using System.Linq;
using HotChocolate.Configuration;
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

        public ConnectionType(
            Action<IObjectTypeDescriptor<Connection>> configure)
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
                .Type<ListType<T>>();
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
}
