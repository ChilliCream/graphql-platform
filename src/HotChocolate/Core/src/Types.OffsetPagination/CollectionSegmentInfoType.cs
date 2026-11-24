using HotChocolate.Types.Composite;
using HotChocolate.Types.Descriptors.Configurations;

namespace HotChocolate.Types.Pagination;

/// <summary>
/// Specifies the page info for a <see cref="CollectionSegment"/>.
/// </summary>
public class CollectionSegmentInfoType : ObjectType<CollectionSegmentInfo>
{
    protected override void Configure(IObjectTypeDescriptor<CollectionSegmentInfo> descriptor)
    {
        if (descriptor.Extend().Context.Options.ApplyShareableToCollectionSegmentInfo)
        {
            descriptor.Directive(Shareable.Instance);
        }

        descriptor
            .Name("CollectionSegmentInfo")
            .Description("Information about the offset pagination.")
            .BindFieldsExplicitly();

        descriptor
            .Field(t => t.HasNextPageAsync(default))
            .Type<NonNullType<BooleanType>>()
            .Name("hasNextPage")
            .Description(
                "Indicates whether more items exist following "
                + "the set defined by the clients arguments.")
            .Extend()
            .Configuration.Flags |= CoreFieldFlags.NotDataResolver;

        descriptor
            .Field(t => t.HasPreviousPageAsync(default))
            .Type<NonNullType<BooleanType>>()
            .Name("hasPreviousPage")
            .Description(
                "Indicates whether more items exist prior "
                + "the set defined by the clients arguments.")
            .Extend()
            .Configuration.Flags |= CoreFieldFlags.NotDataResolver;
    }
}
