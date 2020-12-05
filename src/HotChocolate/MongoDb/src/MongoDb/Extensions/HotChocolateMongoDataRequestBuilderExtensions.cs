using HotChocolate.Execution.Configuration;
using HotChocolate.MongoDb.Data;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Provides data extensions for the <see cref="IRequestExecutorBuilder"/>.
    /// </summary>
    public static class HotChocolateMongoDataRequestBuilderExtensions
    {
        /// <summary>
        /// Adds filtering support.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="IRequestExecutorBuilder"/>.
        /// </param>
        /// <returns>
        /// Returns the <see cref="IRequestExecutorBuilder"/>.
        /// </returns>
        public static IRequestExecutorBuilder AddMongoDbFiltering(
            this IRequestExecutorBuilder builder,
            string? name = null) =>
            builder.ConfigureSchema(s => s.AddMongoDbFiltering(name));

        /// <summary>
        /// Adds sorting support.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="IRequestExecutorBuilder"/>.
        /// </param>
        /// <returns>
        /// Returns the <see cref="IRequestExecutorBuilder"/>.
        /// </returns>
        public static IRequestExecutorBuilder AddMongoDbSorting(
            this IRequestExecutorBuilder builder,
            string? name = null) =>
            builder.ConfigureSchema(s => s.AddMongoDbSorting(name));

        /// <summary>
        /// Adds projections support.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="IRequestExecutorBuilder"/>.
        /// </param>
        /// <returns>
        /// Returns the <see cref="IRequestExecutorBuilder"/>.
        /// </returns>
        public static IRequestExecutorBuilder AddMongoDbProjections(
            this IRequestExecutorBuilder builder,
            string? name = null) =>
            builder.ConfigureSchema(s => s.AddMongoDbProjections(name));
    }
}
