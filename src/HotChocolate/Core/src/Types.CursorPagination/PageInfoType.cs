namespace HotChocolate.Types.Pagination;

public class PageInfoType : ObjectType<ConnectionPageInfo>
{
    protected override void Configure(
        IObjectTypeDescriptor<ConnectionPageInfo> descriptor)
    {
        descriptor
            .Name(Names.PageInfo)
            .Description("Information about pagination in a connection.")
            .BindFields(BindingBehavior.Explicit);

        descriptor
            .Field(t => t.HasNextPage)
            .Type<NonNullType<BooleanType>>()
            .Name(Names.HasNextPage)
            .Description(
                "Indicates whether more edges exist following " +
                "the set defined by the clients arguments.");

        descriptor
            .Field(t => t.HasPreviousPage)
            .Type<NonNullType<BooleanType>>()
            .Name(Names.HasPreviousPage)
            .Description(
                "Indicates whether more edges exist prior " +
                "the set defined by the clients arguments.");

        descriptor
            .Field(t => t.StartCursor)
            .Type<StringType>()
            .Name(Names.StartCursor)
            .Description("When paginating backwards, the cursor to continue.");

        descriptor
            .Field(t => t.EndCursor)
            .Type<StringType>()
            .Name(Names.EndCursor)
            .Description("When paginating forwards, the cursor to continue.");
    }

    public static class Names
    {
        public const string PageInfo = "PageInfo";
        public const string HasNextPage = "hasNextPage";
        public const string HasPreviousPage = "hasPreviousPage";
        public const string StartCursor = "startCursor";
        public const string EndCursor = "endCursor";
    }
}
