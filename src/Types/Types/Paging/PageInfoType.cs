namespace HotChocolate.Types.Paging
{
    public class PageInfoType
        : ObjectType<IPageInfo>
    {
        protected override void Configure(IObjectTypeDescriptor<IPageInfo> descriptor)
        {
            descriptor.Name("PageInfo");

            descriptor.Field(t => t.HasNextPageAsync(default))
                .Type<NonNullType<BooleanType>>();

            descriptor.Field(t => t.HasPreviousAsync(default))
                .Type<NonNullType<BooleanType>>();
        }
    }
}
