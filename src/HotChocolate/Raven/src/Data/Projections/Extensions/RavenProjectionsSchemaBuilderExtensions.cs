namespace HotChocolate.Data;

/// <summary>
/// Common extensions of <see cref="ISchemaBuilder"/> for RavenDB
/// </summary>
public static class RavenProjectionsSchemaBuilderExtensions
{
    /// <summary>
    /// Adds projections for RavenDB to the schema
    /// </summary>
    /// <param name="schemaBuilder">The schema builder</param>
    /// <returns>The schema builder of parameter <paramref name="schemaBuilder"/></returns>
    public static ISchemaBuilder AddRavenProjections(this ISchemaBuilder schemaBuilder)
        => schemaBuilder.AddProjections<RavenProjectionConvention>();
}
