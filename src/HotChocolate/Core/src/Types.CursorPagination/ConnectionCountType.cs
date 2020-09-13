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
            Action<IObjectTypeDescriptor<Connection>> configure)
            : base(descriptor =>
            {
                ApplyConfig(descriptor);
                configure(descriptor);
            })
        {
        }

        protected override void Configure(IObjectTypeDescriptor<Connection> descriptor) =>
            ApplyConfig(descriptor);

        protected static new void ApplyConfig(IObjectTypeDescriptor<Connection> descriptor)
        {
            ConnectionType<T>.ApplyConfig(descriptor);

            descriptor.Field("totalCount")
                .Type<NonNullType<IntType>>()
                .Resolver(GetTotalCount);
        }

        private static long GetTotalCount(
            IResolverContext context)
        {
            Connection connection = context.Parent<Connection>();

            if (connection.Info.TotalCount.HasValue)
            {
                return connection.Info.TotalCount.Value;
            }

            throw new GraphQLException(
                "The total count was not provided by the connection.");
        }
    }
}
