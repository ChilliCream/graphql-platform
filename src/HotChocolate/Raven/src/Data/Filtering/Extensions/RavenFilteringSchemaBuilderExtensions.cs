namespace HotChocolate.Data;

public static class RavenFilteringSchemaBuilderExtensions
{
    public static ISchemaBuilder AddRavenFiltering(this ISchemaBuilder schemaBuilder)
        => schemaBuilder.AddFiltering<RavenConvention>();
}
