namespace HotChocolate.Types.Paging
{
    public class PageInfoType
        : ObjectType<IPageInfo>
    {
        protected override void Configure(IObjectTypeDescriptor<IPageInfo> descriptor)
        {
            descriptor.Name("PageInfo");

            descriptor.Field(t => t.HasNextPage)
                .Type<NonNullType<BooleanType>>()
                .Name("hasNextPage")
                .Description(
                    "Indicates whether more edges exist following " +
                    "the set defined by the clients arguments.");

            descriptor.Field(t => t.HasPreviousPage)
                .Type<NonNullType<BooleanType>>()
                .Name("hasPreviousPage")
                .Description(
                    "Indicates whether more edges exist prior " +
                    "the set defined by the clients arguments.");

            descriptor.Field(t => t.StartCursor)
                .Type<StringType>()
                .Name("startCursor");

            descriptor.Field(t => t.EndCursor)
                .Type<StringType>()
                .Name("endCursor");
        }
    }
}
