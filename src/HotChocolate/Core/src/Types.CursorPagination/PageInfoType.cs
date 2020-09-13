namespace HotChocolate.Types.Pagination
{
    public class PageInfoType : ObjectType<ConnectionPageInfo>
    {
        protected override void Configure(
            IObjectTypeDescriptor<ConnectionPageInfo> descriptor)
        {
            descriptor
                .Name("PageInfo")
                .Description("Information about pagination in a connection.")
                .BindFields(BindingBehavior.Explicit);

            descriptor
                .Field(t => t.HasNextPage)
                .Type<NonNullType<BooleanType>>()
                .Name("hasNextPage")
                .Description(
                    "Indicates whether more edges exist following " +
                    "the set defined by the clients arguments.");

            descriptor
                .Field(t => t.HasPreviousPage)
                .Type<NonNullType<BooleanType>>()
                .Name("hasPreviousPage")
                .Description(
                    "Indicates whether more edges exist prior " +
                    "the set defined by the clients arguments.");

            descriptor
                .Field(t => t.StartCursor)
                .Type<StringType>()
                .Name("startCursor")
                .Description("When paginating backwards, the cursor to continue.");

            descriptor
                .Field(t => t.EndCursor)
                .Type<StringType>()
                .Name("endCursor")
                .Description("When paginating forwards, the cursor to continue.");
        }
    }
}
