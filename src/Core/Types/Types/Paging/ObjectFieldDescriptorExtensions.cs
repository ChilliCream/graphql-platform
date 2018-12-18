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
                .Argument("first", a => a.Type<IntType>())
                .Argument("after", a => a.Type<StringType>())
                .Argument("last", a => a.Type<IntType>())
                .Argument("before", a => a.Type<StringType>());
        }
    }
}
