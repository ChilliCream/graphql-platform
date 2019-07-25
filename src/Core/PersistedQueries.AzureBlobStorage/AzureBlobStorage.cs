using HotChocolate.Execution;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace HotChocolate.PersistedQueries.AzureBlobStorage
{
    /// <summary>
    /// An implementation of <see cref="IReadStoredQueries"/> that
    /// uses Azure Blob Storage as a storage medium.
    /// </summary>
    public class AzureBlobStorage : IReadStoredQueries
    {
        private const string ContainerNotFoundErrorCode = "ContainerNotFound";
        private const string BlobNotFoundErrorCode = "BlobNotFound";
        
        private readonly CloudBlobContainer _container;
        private readonly IQueryRequestBuilder _queryRequestBuilder;

        public AzureBlobStorage(CloudBlobClient azureClient,
            string containerName,
            IQueryRequestBuilder queryRequestBuilder)
        {
            if (azureClient == null)
            {
                throw new ArgumentNullException(nameof(azureClient));
            }

            if (string.IsNullOrWhiteSpace(containerName))
            {
                throw new ArgumentNullException(nameof(containerName));
            }
            
            _container = azureClient.GetContainerReference(containerName);
            _queryRequestBuilder = queryRequestBuilder ?? throw new ArgumentNullException(nameof(queryRequestBuilder));
        }
        
        /// <inheritdoc />
        public async Task<IQuery> ReadQueryAsync(string queryId)
        {
            if (string.IsNullOrWhiteSpace(queryId))
            {
                throw new ArgumentNullException(nameof(queryId));
            }
            
            var queryBlob = await GetBlob(queryId);
            var blobStream = await queryBlob.OpenReadAsync();
            
            using (var streamReader = new StreamReader(blobStream))
            {
                var content = await streamReader.ReadToEndAsync();
                var query = _queryRequestBuilder.SetQuery(content)
                    .Create();

                return query.Query;
            }
        }

        private async Task<ICloudBlob> GetBlob(string queryId)
        {
            try
            {
                return await _container.GetBlobReferenceFromServerAsync($"{queryId}.graphql");
            }
            catch (StorageException e)
            {
                if (e.RequestInformation.ErrorCode == ContainerNotFoundErrorCode)
                {
                    throw new QueryContainerNotFoundException(_container.Name);
                }
                
                if (e.RequestInformation.ErrorCode == BlobNotFoundErrorCode)
                {
                    throw new QueryNotFoundException(queryId);
                }

                // TODO: Do we need an some other exception to wrap the error?
                throw;
            }
        }
    }
}
