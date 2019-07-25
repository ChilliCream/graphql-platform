using System;

namespace HotChocolate.PersistedQueries.AzureBlobStorage
{
    /// <summary>
    /// The exception is thrown when the Azure Blob container
    /// specified for the <see cref="AzureBlobStorage"/> cannot
    /// be found when locating the query file blob.
    /// </summary>
    public class QueryContainerNotFoundException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="containerName">The Azure Blob container name.</param>
        public QueryContainerNotFoundException(string containerName) :
            base($"Unable to locate Azure Blob container '{containerName}'.")
        {
        }
        
        /// <summary>
        /// The name of the container that could not be found.
        /// </summary>
        public string ContainerName { get; }
    }
}
