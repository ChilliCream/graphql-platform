namespace HotChocolate.Types.Pagination
{
    /// <summary>
    /// Specifies the page info for a <see cref="CollectionSegment"/>.
    /// </summary>
    public class CollectionSegmentInfoType : ObjectType<CollectionSegmentInfo>
    {
        protected override void Configure(IObjectTypeDescriptor<CollectionSegmentInfo> descriptor)
        {
            descriptor
                .Name("CollectionSegmentInfo")
                .Description("Information about the offset pagination.")
                .BindFieldsExplicitly();

            descriptor
                .Field(t => t.HasNextPage)
                .Type<NonNullType<BooleanType>>()
                .Name("hasNextPage")
                .Description(
                    "Indicates whether more items exist following " +
                    "the set defined by the clients arguments.");

            descriptor
                .Field(t => t.HasPreviousPage)
                .Type<NonNullType<BooleanType>>()
                .Name("hasPreviousPage")
                .Description(
                    "Indicates whether more items exist prior " +
                    "the set defined by the clients arguments.");
        }
    }
}
