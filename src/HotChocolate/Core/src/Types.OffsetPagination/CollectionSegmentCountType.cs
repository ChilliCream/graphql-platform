using System;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Types.Pagination
{
    public class CollectionSegmentCountType<T>
        : CollectionSegmentType<T>
        where T : class, IOutputType
    {
        public CollectionSegmentCountType()
        {
        }

        public CollectionSegmentCountType(
            Action<IObjectTypeDescriptor<CollectionSegment>> configure)
            : base(descriptor =>
            {
                ApplyConfig(descriptor);
                configure(descriptor);
            })
        {
        }

        protected override void Configure(IObjectTypeDescriptor<CollectionSegment> descriptor) =>
            ApplyConfig(descriptor);

        protected static new void ApplyConfig(IObjectTypeDescriptor<CollectionSegment> descriptor)
        {
            CollectionSegmentType<T>.ApplyConfig(descriptor);

            descriptor
                .Field("totalCount")
                .Type<NonNullType<IntType>>()
                .ResolveWith<Resolvers>(t => t.GetTotalCount(default!, default));
        }

        private sealed class Resolvers
        {
            public ValueTask<int> GetTotalCount(
                [Parent] CollectionSegment segment,
                CancellationToken cancellationToken) =>
                segment.GetTotalCountAsync(cancellationToken);
        }
    }
}
