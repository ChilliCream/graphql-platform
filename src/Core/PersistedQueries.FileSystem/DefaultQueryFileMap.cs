namespace HotChocolate.PersistedQueries.FileSystem
{
    /// <summary>
    /// A default implementation of <see cref="IQueryFileMap"/>.
    /// </summary>
    public class DefaultQueryFileMap : IQueryFileMap
    {
        /// <inheritdoc />
        public string MapToFilePath(string queryIdentifier)
        {
            // TODO: Determine what we want the default query file map to do.
            throw new System.NotImplementedException();
        }
    }
}
