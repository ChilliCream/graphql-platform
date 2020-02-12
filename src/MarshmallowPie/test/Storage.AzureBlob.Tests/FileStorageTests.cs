using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Storage.Blob;
using Squadron;
using Xunit;

namespace MarshmallowPie.Storage.AzureBlob
{
    public class FileStorageTests
        : IClassFixture<AzureStorageBlobResource>
    {
        private readonly AzureStorageBlobResource _azureStorageResource;

        public FileStorageTests(AzureStorageBlobResource azureStorageResource)
        {
            _azureStorageResource = azureStorageResource;
        }

        [Fact]
        public async Task CreateTextFile()
        {
            // arrange
            CloudBlobClient client = _azureStorageResource.CreateBlobClient();
            string containerName = Guid.NewGuid().ToString("N");
            await client.GetContainerReference(containerName).CreateIfNotExistsAsync();

            var storage = new FileStorage(client, containerName);
            IFileContainer container = await storage.CreateContainerAsync("abc");

            // act
            await container.CreateTextFileAsync("def", "ghi");

            // assert
            IListBlobItem item = client.GetContainerReference(containerName)
                .GetDirectoryReference("abc")
                .ListBlobs()
                .SingleOrDefault();

            Assert.NotNull(item);
            Assert.Equal(
                $"/devstoreaccount1/{containerName}/abc/def",
                item.Uri.LocalPath);

            byte[] buffer = new byte[12];

            int buffered = await client.GetContainerReference(containerName)
                .GetDirectoryReference("abc")
                .GetBlobReference("def")
                .DownloadToByteArrayAsync(buffer, 0);

            Assert.Equal("ghi", Encoding.UTF8.GetString(buffer, 0, buffered));
        }

        [Fact]
        public async Task DeleteTextFile()
        {
            // arrange
            CloudBlobClient client = _azureStorageResource.CreateBlobClient();
            string containerName = Guid.NewGuid().ToString("N");
            await client.GetContainerReference(containerName).CreateIfNotExistsAsync();

            var storage = new FileStorage(client, containerName);
            IFileContainer container = await storage.CreateContainerAsync("abc");
            await container.CreateTextFileAsync("def", "ghi");

            IListBlobItem item = client.GetContainerReference(containerName)
                .GetDirectoryReference("abc")
                .ListBlobs()
                .SingleOrDefault();

            Assert.NotNull(item);

            // act
            IFile file = await container.GetFileAsync("def");
            await file.DeleteAsync();

            // assert
            Assert.False(client.GetContainerReference(containerName)
                .GetDirectoryReference("abc")
                .ListBlobs()
                .Any());
        }

        [Fact]
        public async Task DeleteContainer()
        {
            // arrange
            CloudBlobClient client = _azureStorageResource.CreateBlobClient();
            string containerName = Guid.NewGuid().ToString("N");
            await client.GetContainerReference(containerName).CreateIfNotExistsAsync();

            var storage = new FileStorage(client, containerName);
            IFileContainer container = await storage.CreateContainerAsync("abc");
            await container.CreateTextFileAsync("def", "ghi");

            IListBlobItem item = client.GetContainerReference(containerName)
                .GetDirectoryReference("abc")
                .ListBlobs()
                .SingleOrDefault();

            Assert.NotNull(item);

            // act
            await container.DeleteAsync();

            // assert
            Assert.False(client.GetContainerReference(containerName)
                .GetDirectoryReference("abc")
                .ListBlobs()
                .Any());
        }

        [Fact]
        public async Task ContainerExists()

        {
            // arrange
            CloudBlobClient client = _azureStorageResource.CreateBlobClient();
            string containerName = Guid.NewGuid().ToString("N");
            await client.GetContainerReference(containerName).CreateIfNotExistsAsync();

            var storage = new FileStorage(client, containerName);
            IFileContainer container = await storage.CreateContainerAsync("abc");
            await container.CreateTextFileAsync("def", "ghi");

            IListBlobItem item = client.GetContainerReference(containerName)
                .GetDirectoryReference("abc")
                .ListBlobs()
                .SingleOrDefault();

            Assert.NotNull(item);

            // act
            bool result = await storage.ContainerExistsAsync("abc");

            // assert
            Assert.True(result);
        }
    }
}
