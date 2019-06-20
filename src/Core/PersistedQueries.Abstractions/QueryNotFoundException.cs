using System;

namespace HotChocolate.PersistedQueries
{
    /// <summary>
    /// Represents a query that could not be found.
    /// </summary>
    public class QueryNotFoundException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="queryIdentifier">The query identifier that could not be found.</param>
        public QueryNotFoundException(string queryIdentifier) :
            base($"Unable to find query with identifier '{queryIdentifier}'.")
        {
        }
    }
}
