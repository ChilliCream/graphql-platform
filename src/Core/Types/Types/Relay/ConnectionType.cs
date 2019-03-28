using System;
using HotChocolate.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Relay
{
    public class ConnectionType<T>
        : ObjectType<IConnection>
        , IConnectionType
        where T : IOutputType, new()
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
        }

        protected override void OnRegisterDependencies(
            IInitializationContext context,
            ObjectTypeDefinition definition)
        {
            base.OnRegisterDependencies(context, definition);

            context.RegisterDependency(new ClrTypeReference(
                typeof(T), TypeContext.Output),
                TypeDependencyKind.Named);
            context.RegisterDependency(new ClrTypeReference(
                typeof(EdgeType<T>), TypeContext.Output),
                TypeDependencyKind.Default);
        }

        protected override void OnCompleteName(
            ICompletionContext context,
            ObjectTypeDefinition definition)
        {
            base.OnCompleteName(context, definition);

            INamedType namedType = context.GetType<INamedType>(
                new ClrTypeReference(typeof(T), TypeContext.Output));

            Name = namedType.Name + "Connection";
        }

        protected override void OnCompleteType(
            ICompletionContext context,
            ObjectTypeDefinition definition)
        {
            base.OnCompleteType(context, definition);

            EdgeType = context.GetType<EdgeType<T>>(
                new ClrTypeReference(typeof(EdgeType<T>),
                TypeContext.Output));
        }

        public static ConnectionType<T> CreateWithTotalCount()
        {
            return new ConnectionType<T>(c =>
            {
                c.Field("totalCount")
                    .Type<NonNullType<IntType>>()
                    .Resolver(ctx => GetTotalCount(ctx));
            });
        }

        private static IResolverResult<long> GetTotalCount(
            IResolverContext context)
        {
            IConnection connection = context.Parent<IConnection>();
            if (connection.PageInfo.TotalCount.HasValue)
            {
                return ResolverResult.CreateValue(
                    connection.PageInfo.TotalCount.Value);
            }
            return ResolverResult.CreateError<long>(
                "The total count was not provided by the connection.");
        }
    }
}
