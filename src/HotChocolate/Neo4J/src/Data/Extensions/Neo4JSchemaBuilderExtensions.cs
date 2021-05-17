using HotChocolate.Data.Neo4J.Filtering;
using HotChocolate.Data.Neo4J.Projections;
using HotChocolate.Data.Neo4J.Sorting;

namespace HotChocolate.Data.Neo4J
{
    /// <summary>
    /// Provides Neo4j extensions for the <see cref="ISchemaBuilder"/>.
    /// </summary>
    public static class Neo4JSchemaBuilderExtensions
    {
        /// <summary>
        /// Adds filtering support for Neo4j.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="ISchemaBuilder"/>.
        /// </param>
        /// <param name="name">
        /// The filtering convention name.
        /// </param>
        /// <returns>
        /// Returns the <see cref="ISchemaBuilder"/>.
        /// </returns>
        public static ISchemaBuilder AddNeo4JFiltering(
            this ISchemaBuilder builder,
            string? name = null) =>
            builder.AddFiltering(x => x.AddNeo4JDefaults(), name);

        /// <summary>
        /// Adds sorting support for Neo4j.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="ISchemaBuilder"/>.
        /// </param>
        /// <param name="name">
        /// The sorting convention name.
        /// </param>
        /// <returns>
        /// Returns the <see cref="ISchemaBuilder"/>.
        /// </returns>
        public static ISchemaBuilder AddNeo4JSorting(
            this ISchemaBuilder builder,
            string? name = null) =>
            builder.AddSorting(x => x.AddNeo4JDefaults(), name);

        /// <summary>
        /// Adds projections support for Neo4j.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="ISchemaBuilder"/>.
        /// </param>
        /// <param name="name">
        /// The projections convention name.
        /// </param>
        /// <returns>
        /// Returns the <see cref="ISchemaBuilder"/>.
        /// </returns>
        public static ISchemaBuilder AddNeo4JProjections(
            this ISchemaBuilder builder,
            string? name = null) =>
            builder.AddProjections(x => x.AddNeo4JDefaults(), name);
    }
}
