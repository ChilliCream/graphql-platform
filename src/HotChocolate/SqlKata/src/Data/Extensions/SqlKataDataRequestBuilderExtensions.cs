using HotChocolate.Execution.Configuration;
using HotChocolate.Data.SqlKata;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Provides data extensions for the <see cref="IRequestExecutorBuilder"/>.
    /// </summary>
    public static class SqlKataDataRequestBuilderExtensions
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
        public static IRequestExecutorBuilder AddSqlKataFiltering(
            this IRequestExecutorBuilder builder,
            string? name = null) =>
            builder.ConfigureSchema(s => s.AddSqlKataFiltering(name));

        /*
        /// <summary>
        /// Adds sorting support.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="IRequestExecutorBuilder"/>.
        /// </param>
        /// <returns>
        /// Returns the <see cref="IRequestExecutorBuilder"/>.
        /// </returns>
        public static IRequestExecutorBuilder AddSqlKataSorting(
            this IRequestExecutorBuilder builder,
            string? name = null) =>
            builder.ConfigureSchema(s => s.AddSqlKataSorting(name));

        /// <summary>
        /// Adds projections support.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="IRequestExecutorBuilder"/>.
        /// </param>
        /// <returns>
        /// Returns the <see cref="IRequestExecutorBuilder"/>.
        /// </returns>
        public static IRequestExecutorBuilder AddSqlKataProjections(
            this IRequestExecutorBuilder builder,
            string? name = null) =>
            builder.ConfigureSchema(s => s.AddSqlKataProjections(name));
    */
    }
}
