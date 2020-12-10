namespace HotChocolate.Data.MongoDb.Execution
{
    /// <summary>
    /// Represents a executable for the MongoDb.
    /// </summary>
    public interface IMongoExecutable : IExecutable
    {
        /// <summary>
        /// Applies the filter definition to the executable
        /// </summary>
        /// <param name="filters">The filter definition</param>
        /// <returns>A executable that contains the filter definition</returns>
        IMongoExecutable WithFiltering(MongoDbFilterDefinition filters);

        /// <summary>
        /// Applies the sorting definition to the executable
        /// </summary>
        /// <param name="sorting">The sorting definition</param>
        /// <returns>A executable that contains the sorting definition</returns>
        IMongoExecutable WithSorting(MongoDbSortDefinition sorting);

        /// <summary>
        /// Applies the projection definition to the executable
        /// </summary>
        /// <param name="projection">The projection definition</param>
        /// <returns>A executable that contains the projection definition</returns>
        IMongoExecutable WithProjection(MongoDbProjectionDefinition projection);
    }
}
