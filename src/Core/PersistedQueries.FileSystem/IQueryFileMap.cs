namespace HotChocolate.PersistedQueries.FileSystem
{
    /// <summary>
    /// Responsible for mapping a query identifier to a file path.
    /// </summary>
    public interface IQueryFileMap
    {
        /// <summary>
        /// Gets the roor directory on which the map operates.
        /// </summary>
        string Root { get; }

        /// <summary>
        /// Maps a query identifier to the file path
        /// containing the query.
        /// </summary>
        /// <param name="queryId">The query identifier.</param>
        /// <returns>The file path of the query.</returns>
        string MapToFilePath(string queryId);
    }
}
