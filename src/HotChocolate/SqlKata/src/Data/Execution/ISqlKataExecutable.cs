using SqlKata;

namespace HotChocolate.Data.SqlKata
{
    /// <summary>
    /// Represents a executable for the SqlKata.
    /// </summary>
    public interface ISqlKataExecutable : IExecutable
    {
        /// <summary>
        /// Applies the filter definition to the executable
        /// </summary>
        /// <param name="filters">The filter definition</param>
        /// <returns>A executable that contains the filter definition</returns>
        ISqlKataExecutable WithFiltering(Query filters);

        /// <summary>
        /// Applies the sorting definition to the executable
        /// </summary>
        /// <param name="sorting">The sorting definition</param>
        /// <returns>A executable that contains the sorting definition</returns>
        ISqlKataExecutable WithSorting(Query sorting);

        /// <summary>
        /// Applies the projection definition to the executable
        /// </summary>
        /// <param name="projection">The projection definition</param>
        /// <returns>A executable that contains the projection definition</returns>
        ISqlKataExecutable WithProjection(Query projection);
    }
}
