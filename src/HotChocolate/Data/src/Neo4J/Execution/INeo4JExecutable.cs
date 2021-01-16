using HotChocolate.Data.Neo4J.Driver;

namespace HotChocolate.Data.Neo4J.Execution
{
    /// <summary>
    /// Represents a executable for Neo4j database.
    /// </summary>
    public interface INeo4JExecutable : IExecutable
    {
        /// <summary>
        /// Applies the filter definition to the executable
        /// </summary>
        /// <param name="filters">The filter definition</param>
        /// <returns>A executable that contains the filter definition</returns>
        INeo4JExecutable WithFiltering(Neo4JFilterDefinition filters);

        /// <summary>
        /// Applies the sorting definition to the executable
        /// </summary>
        /// <param name="sorting">The sorting definition</param>
        /// <returns>A executable that contains the sorting definition</returns>
        INeo4JExecutable WithSorting(Neo4JSortDefinition sorting);

        /// <summary>
        /// Applies the projection definition to the executable
        /// </summary>
        /// <param name="projection">The projection definition</param>
        /// <returns>A executable that contains the projection definition</returns>
        INeo4JExecutable WithProjection(Neo4JProjectionDefinition projection);
    }
}
