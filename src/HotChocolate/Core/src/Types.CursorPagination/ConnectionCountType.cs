using System;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Types.Pagination
{
    public class ConnectionCountType<T>
        : ConnectionType<T>
        where T : class, IOutputType
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

            descriptor
                .Field("totalCount")
                .Type<NonNullType<IntType>>()
                .ResolveWith<Resolvers>(t => t.GetTotalCount(default!, default));
        }

        private sealed class Resolvers
        {
            public ValueTask<int> GetTotalCount(
                [Parent] Connection connection,
                CancellationToken cancellationToken) =>
                connection.GetTotalCountAsync(cancellationToken);
        }
    }
}
