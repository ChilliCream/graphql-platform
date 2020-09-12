using System;
using HotChocolate.Resolvers;

namespace HotChocolate.Types.Pagination
{
    public class ConnectionCountType<T> : ConnectionType<T> where T : class, IOutputType
    {
        public ConnectionCountType()
        {
        }

        public ConnectionCountType(
            Action<IObjectTypeDescriptor<IConnection>> configure)
            : base(descriptor =>
            {
                ApplyConfig(descriptor);
                configure(descriptor);
            })
        {
        }

        protected override void Configure(IObjectTypeDescriptor<IConnection> descriptor) =>
            ApplyConfig(descriptor);

        protected static new void ApplyConfig(IObjectTypeDescriptor<IConnection> descriptor)
        {
            ConnectionType<T>.ApplyConfig(descriptor);

            descriptor.Field("totalCount")
                .Type<NonNullType<IntType>>()
                .Resolver(GetTotalCount);
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
