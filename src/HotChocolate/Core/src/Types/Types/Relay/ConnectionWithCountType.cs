using System;
using HotChocolate.Resolvers;

namespace HotChocolate.Types.Relay
{
    public class ConnectionWithCountType<T>
        : ConnectionType<T>
        where T : class, IOutputType
    {
        public ConnectionWithCountType()
            : base(descriptor => Configure(descriptor))
        {
        }

        public ConnectionWithCountType(
            Action<IObjectTypeDescriptor<IConnection>> configure)
            : base(descriptor =>
            {
                Configure(descriptor);
                configure?.Invoke(descriptor);
            })
        {
        }

        protected new static void Configure(
            IObjectTypeDescriptor<IConnection> descriptor)
        {
            descriptor.Field("totalCount")
                .Type<NonNullType<IntType>>()
                .Resolver(ctx => GetTotalCount(ctx));
        }

        private static long GetTotalCount(
            IResolverContext context)
        {
            IConnection connection = context.Parent<IConnection>();
            if (connection.PageInfo.TotalCount.HasValue)
            {
                return connection.PageInfo.TotalCount.Value;
            }
            
            throw new GraphQLException(
                "The total count was not provided by the connection.");
        }
    }
}
