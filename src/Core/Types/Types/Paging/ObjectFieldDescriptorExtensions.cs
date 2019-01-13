namespace HotChocolate.Types.Paging
{
    public static class ObjectFieldDescriptorExtensions
    {
        public static IObjectFieldDescriptor UsePaging<TSchemaType, TClrType>(
            this IObjectFieldDescriptor descriptor)
            where TSchemaType : INamedOutputType, new()
        {
            return descriptor
                .AddPagingArguments()
                .Type<ConnectionType<TSchemaType>>()
                .Use<QueryableConnectionMiddleware<TClrType>>();
        }

        public static IObjectFieldDescriptor AddPagingArguments(
            this IObjectFieldDescriptor descriptor)
        {
            return descriptor
                .Argument("first", a => a.Type<PaginationAmountType>())
                .Argument("after", a => a.Type<StringType>())
                .Argument("last", a => a.Type<PaginationAmountType>())
                .Argument("before", a => a.Type<StringType>());
        }
    }
}
