using HotChocolate.Data.Raven.Sorting;

namespace HotChocolate.Data;

/// <summary>
/// Common extensions of <see cref="ISchemaBuilder"/> for RavenDB
/// </summary>
public static class RavenSortingSchemaBuilderExtensions
{
    /// <summary>
    /// Adds sorting for RavenDB to the schema
    /// </summary>
    /// <param name="schemaBuilder">The schema builder</param>
    /// <returns>The schema builder of parameter <paramref name="schemaBuilder"/></returns>
    public static ISchemaBuilder AddRavenSorting(this ISchemaBuilder schemaBuilder)
    {
        return schemaBuilder.AddSorting(x => x
            .AddDefaultOperations()
            .BindDefaultTypes()
            .Provider(new RavenQueryableSortProvider(x => x.AddDefaultFieldHandlers())));
    }
}
