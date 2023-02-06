namespace HotChocolate.Data;

public static class RavenProjectionsSchemaBuilderExtensions
{
    public static ISchemaBuilder AddRavenProjections(this ISchemaBuilder schemaBuilder)
        => schemaBuilder.AddProjections<RavenProjectionConvention>();
}
