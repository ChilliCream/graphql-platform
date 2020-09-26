using System;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Types.Pagination
{
    /// <summary>
    /// The collection segment type with additional total count field.
    /// </summary>
    /// <typeparam name="T">
    /// The item type.
    /// </typeparam>
    public class CollectionSegmentCountType<T>
        : CollectionSegmentType<T>
        where T : class, IOutputType
    {
        /// <summary>
        /// Initializes <see cref="CollectionSegmentCountType{T}" />.
        /// </summary>
        public CollectionSegmentCountType()
        {
        }

        /// <summary>
        /// Initializes <see cref="CollectionSegmentCountType{T}" />.
        /// </summary>
        /// <param name="configure">
        /// A delegate adding more configuration to the type.
        /// </param>
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
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            CollectionSegmentType<T>.ApplyConfig(descriptor);

            descriptor
                .Field(OffsetPagingFieldNames.TotalCount)
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
