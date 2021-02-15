using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data.Neo4J.Extensions
{
    /// <summary>
    /// Provides data extensions for the <see cref="IRequestExecutorBuilder"/>.
    /// </summary>
    public static class Neo4JDataRequestBuilderExtensions
    {
        /// <summary>
        /// Adds filtering support for Neo4j.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="IRequestExecutorBuilder"/>.
        /// </param>
        /// <param name="name">
        /// The filtering convention name.
        /// </param>
        /// <returns>
        /// Returns the <see cref="IRequestExecutorBuilder"/>.
        /// </returns>
        public static IRequestExecutorBuilder AddNeo4JFiltering(
            this IRequestExecutorBuilder builder,
            string? name = null) =>
            builder.ConfigureSchema(s => s.AddNeo4JFiltering(name));

        // TODO: Implement sorting RequestExecutorBuilder Extension
        /// <summary>
        /// Adds sorting support for Neo4j.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="IRequestExecutorBuilder"/>.
        /// </param>
        /// <param name="name">
        /// The sorting convention name.
        /// </param>
        /// <returns>
        /// Returns the <see cref="IRequestExecutorBuilder"/>.
        /// </returns>
        public static IRequestExecutorBuilder AddNeo4JSorting(
             this IRequestExecutorBuilder builder,
             string? name = null) =>
             builder.ConfigureSchema(s => s.AddNeo4JSorting(name));

        /// <summary>
        /// Adds projections support for Neo4j.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="IRequestExecutorBuilder"/>.
        /// </param>
        /// <param name="name">
        /// The projection convention name.
        /// </param>
        /// <returns>
        /// Returns the <see cref="IRequestExecutorBuilder"/>.
        /// </returns>
        public static IRequestExecutorBuilder AddNeo4JProjections(
            this IRequestExecutorBuilder builder,
            string? name = null) =>
            builder.ConfigureSchema(s => s.AddNeo4JProjections(name));
    }
}
