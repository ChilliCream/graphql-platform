using System;

namespace HotChocolate.PersistedQueries
{
    /// <summary>
    /// The exception is thrown when the specified query Id
    /// is not able to be found in the persistence medium.
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
            QueryIdentifier = queryIdentifier;
        }
        
        /// <summary>
        /// The query identifier that could not be found.
        /// </summary>
        public string QueryIdentifier { get; }
    }
}
