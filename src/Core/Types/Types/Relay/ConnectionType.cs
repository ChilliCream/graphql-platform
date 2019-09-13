using System;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Relay
{
    public class ConnectionType<T>
        : ObjectType<IConnection>
        , IConnectionType
        where T : class, IOutputType
    {
        public ConnectionType()
            : base(descriptor => Configure(descriptor))
        {
        }

        public ConnectionType(
            Action<IObjectTypeDescriptor<IConnection>> configure)
            : base(descriptor =>
            {
                Configure(descriptor);
                configure?.Invoke(descriptor);
            })
        {
        }

        public IEdgeType EdgeType { get; private set; }

        protected new static void Configure(
            IObjectTypeDescriptor<IConnection> descriptor)
        {
            descriptor.Name(dependency => dependency.Name + "Connection")
                .DependsOn<T>();

            descriptor.Description("A connection to a list of items.");

            descriptor.BindFields(BindingBehavior.Explicit);

            descriptor.Field(t => t.PageInfo)
                .Name("pageInfo")
                .Description("Information to aid in pagination.")
                .Type<NonNullType<PageInfoType>>();

            descriptor.Field(t => t.Edges)
                .Name("edges")
                .Description("A list of edges.")
                .Type<ListType<NonNullType<EdgeType<T>>>>();

            descriptor.Field("nodes")
                .Description("A flattened list of the nodes.")
                .Type<ListType<T>>()
                .Resolver(ctx =>
                    ctx.Parent<IConnection>().Edges.Select(t => t.Node));
        }

        protected override void OnRegisterDependencies(
            IInitializationContext context,
            ObjectTypeDefinition definition)
        {
            base.OnRegisterDependencies(context, definition);

            context.RegisterDependency(
                ClrTypeReference.FromSchemaType<EdgeType<T>>(),
                TypeDependencyKind.Default);
        }

        protected override void OnCompleteType(
            ICompletionContext context,
            ObjectTypeDefinition definition)
        {
            base.OnCompleteType(context, definition);

            EdgeType = context.GetType<EdgeType<T>>(
                ClrTypeReference.FromSchemaType<EdgeType<T>>());
        }
    }
}
