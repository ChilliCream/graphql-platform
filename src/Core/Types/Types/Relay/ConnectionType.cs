using System;
using HotChocolate.Resolvers;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Relay
{
    public class ConnectionType<T>
        : ObjectType<IConnection>
        , IConnectionType
        where T : IOutputType, new()
    {
        private readonly Action<IObjectTypeDescriptor<IConnection>> _configure;

        public ConnectionType()
        {
        }

        public ConnectionType(
            Action<IObjectTypeDescriptor<IConnection>> configure)
        {
            _configure = configure;
        }

        public IEdgeType EdgeType { get; private set; }

        protected override void Configure(
            IObjectTypeDescriptor<IConnection> descriptor)
        {
            if (!NamedTypeInfoFactory.Default.TryExtractName(
                typeof(T), out NameString name))
            {
                throw new InvalidOperationException(
                    $"Unable to extract a name from {typeof(T).FullName}.");
            }

            descriptor.Name(name + "Connection");
            descriptor.Description("A connection to a list of items.");

            descriptor.Field(t => t.PageInfo)
                .Name("pageInfo")
                .Description("Information to aid in pagination.")
                .Type<NonNullType<PageInfoType>>();

            descriptor.Field(t => t.Edges)
                .Name("edges")
                .Description("A list of edges.")
                .Type<ListType<NonNullType<EdgeType<T>>>>();

            _configure?.Invoke(descriptor);
        }

        protected override void OnRegisterDependencies(
            ITypeInitializationContext context)
        {
            base.OnRegisterDependencies(context);

            context.RegisterType(new TypeReference(typeof(T)));
            context.RegisterType(new TypeReference(typeof(EdgeType<T>)));
        }

        protected override void OnCompleteType(
            ITypeInitializationContext context)
        {
            EdgeType = context.GetType<EdgeType<T>>(
                new TypeReference(typeof(EdgeType<T>)));

            base.OnCompleteType(context);
        }

        public static ConnectionType<T> CreateWithTotalCount()
        {
            return new ConnectionType<T>(c => c
                .Field("totalCount")
                .Type<NonNullType<IntType>>()
                .Resolver(ctx => GetTotalCount(ctx)));
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
