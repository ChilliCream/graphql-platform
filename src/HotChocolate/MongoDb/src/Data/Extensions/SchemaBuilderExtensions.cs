using HotChocolate.Data.Filters;
using HotChocolate.Data.MongoDb.Filters;

namespace HotChocolate.Data.MongoDb
{
    /// <summary>
    /// Provides mongo extensions for the <see cref="ISchemaBuilder"/>.
    /// </summary>
    public static class MongoDbSchemaBuilderExtensions
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
        public static ISchemaBuilder AddMongoDbFiltering(
            this ISchemaBuilder builder,
            string? name = null) =>
            builder.AddFiltering(x => x.AddMongoDbDefaults(), name);

        /// <summary>
        /// Adds sorting support.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="ISchemaBuilder"/>.
        /// </param>
        /// <returns>
        /// Returns the <see cref="ISchemaBuilder"/>.
        /// </returns>
        public static ISchemaBuilder AddMongoDbSorting(
            this ISchemaBuilder builder,
            string? name = null) =>
            builder.AddSorting(x => x.AddMongoDbDefaults(), name);

        /// <summary>
        /// Adds projections support.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="ISchemaBuilder"/>.
        /// </param>
        /// <returns>
        /// Returns the <see cref="ISchemaBuilder"/>.
        /// </returns>
        public static ISchemaBuilder AddMongoDbProjections(
            this ISchemaBuilder builder,
            string? name = null) =>
            builder.AddProjections(x => x.AddMongoDbDefaults(), name);
    }
}
