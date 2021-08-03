using HotChocolate.Data.Filters;
using HotChocolate.Data.SqlKata.Filters;

namespace HotChocolate.Data.SqlKata
{
    /// <summary>
    /// Provides mongo extensions for the <see cref="ISchemaBuilder"/>.
    /// </summary>
    public static class SqlKataSchemaBuilderExtensions
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
        public static ISchemaBuilder AddSqlKataFiltering(
            this ISchemaBuilder builder,
            string? name = null) =>
            builder
                .AddFiltering(x => x.AddSqlKataDefaults(), name)
                .TryAddTypeInterceptor<SqlKataDataAnnotationsTypeInterceptor>();

        /*
        /// <summary>
        /// Adds sorting support.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="ISchemaBuilder"/>.
        /// </param>
        /// <returns>
        /// Returns the <see cref="ISchemaBuilder"/>.
        /// </returns>
        public static ISchemaBuilder AddSqlKataSorting(
            this ISchemaBuilder builder,
            string? name = null) =>
            builder.AddSorting(x => x.AddSqlKataDefaults(), name);

        /// <summary>
        /// Adds projections support.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="ISchemaBuilder"/>.
        /// </param>
        /// <returns>
        /// Returns the <see cref="ISchemaBuilder"/>.
        /// </returns>
        public static ISchemaBuilder AddSqlKataProjections(
            this ISchemaBuilder builder,
            string? name = null) =>
            builder.AddProjections(x => x.AddSqlKataDefaults(), name);
    */
    }
}
