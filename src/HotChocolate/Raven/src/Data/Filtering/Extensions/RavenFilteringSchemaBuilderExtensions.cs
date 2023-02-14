namespace HotChocolate.Data;

/// <summary>
/// Common extensions of <see cref="ISchemaBuilder"/> for RavenDB
/// </summary>
public static class RavenFilteringSchemaBuilderExtensions
{
    /// <summary>
    /// Adds filtering for RavenDB to the schema
    /// </summary>
    /// <param name="schemaBuilder">The schema builder</param>
    /// <returns>The schema builder of parameter <paramref name="schemaBuilder"/></returns>
    public static ISchemaBuilder AddRavenFiltering(this ISchemaBuilder schemaBuilder)
        => schemaBuilder.AddFiltering<RavenFilteringConvention>();
}
