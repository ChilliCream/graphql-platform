namespace HotChocolate.Tests
{
    public static class SchemaBuilderExtensions
    {
        public static ISchemaBuilder UseNothing(this ISchemaBuilder builder) =>
            builder.Use(next => context => default);
    }
}
