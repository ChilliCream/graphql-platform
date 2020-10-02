using HotChocolate.Data.Filters;
using HotChocolate.MongoDb.Data.Filters;

namespace HotChocolate.MongoDb.Data
{
    /// <summary>
    /// Provides filtering extensions for the <see cref="ISchemaBuilder"/>.
    /// </summary>
    public static class MongoSchemaBuilderExtensions
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
            this ISchemaBuilder builder) =>
            builder.AddFiltering(x => x.AddMongoDbDefaults());
    }
}
