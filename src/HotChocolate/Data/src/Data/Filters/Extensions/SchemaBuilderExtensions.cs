namespace HotChocolate.Data.Filters
{
    public static class SchemaBuilderExtensions
    {
        public static ISchemaBuilder UseFiltering(
            this ISchemaBuilder builder) =>
            builder.TryAddConvention<IFilterConvention>(
                (sp) => new FilterConvention(x => x.AddDefaults()))
                .AddTypeInterceptor<FilterTypeInterceptor>();
    }
}
