using HotChocolate.Data.Neo4J.Filtering.Convention.Extensions;

namespace HotChocolate.Data.Neo4J
{
    /// <summary>
    /// Provides Neo4j extensions for the <see cref="ISchemaBuilder"/>.
    /// </summary>
    public static class Neo4JSchemaBuilderExtensions
    {
        /// <summary>
        /// Adds filtering support.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="ISchemaBuilder"/>.
        /// </param>
        /// <returns>
        /// Returns the <see cref="ISchemaBuilder"/>.
        /// </returns>
        public static ISchemaBuilder AddNeo4JFiltering(
            this ISchemaBuilder builder,
            string? name = null) =>
            builder.AddFiltering(x => x.AddNeo4JDefaults(), name);

        /// <summary>
        /// Adds sorting support.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="ISchemaBuilder"/>.
        /// </param>
        /// <returns>
        /// Returns the <see cref="ISchemaBuilder"/>.
        /// </returns>
        public static ISchemaBuilder AddNeo4JSorting(
            this ISchemaBuilder builder,
            string? name = null) =>
            builder.AddSorting(x => x.AddNeo4JDefaults(), name);

        /// <summary>
        /// Adds projections support.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="ISchemaBuilder"/>.
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
