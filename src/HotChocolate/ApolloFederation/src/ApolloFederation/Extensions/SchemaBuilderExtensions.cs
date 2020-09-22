namespace HotChocolate.ApolloFederation.Extensions
{
    public static class SchemaBuilderExtensions
    {
        public static ISchemaBuilder AddApolloFederation(
            this ISchemaBuilder builder)
        {
            builder.AddType<EntityType>();
            builder.AddType<ExternalDirectiveType>();
            builder.AddType<ProvidesDirectiveType>();
            builder.AddType<KeyDirectiveType>();
            builder.AddType<FieldSetType>();
            builder.AddType<RequiresDirectiveType>();
            builder.TryAddTypeInterceptor<EntityTypeInterceptor>();
            return builder;
        }
    }
}
