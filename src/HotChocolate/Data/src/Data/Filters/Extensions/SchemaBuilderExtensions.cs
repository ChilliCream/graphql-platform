namespace HotChocolate.Data.Filters
{
    public static class SchemaBuilderExtensions
    {
        public static ISchemaBuilder UseFiltering(
            this ISchemaBuilder builder) =>
            builder.AddTypeInterceptor<FilterTypeInterceptor>();
    }
}
