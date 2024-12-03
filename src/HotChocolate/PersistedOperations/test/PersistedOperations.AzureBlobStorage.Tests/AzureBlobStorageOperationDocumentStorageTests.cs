using Azure.Storage.Blobs;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;
using Squadron;

namespace HotChocolate.PersistedOperations.AzureBlobStorage;

public class AzureBlobStorageOperationDocumentStorageTests : IClassFixture<AzureStorageBlobResource>
{
    private readonly BlobContainerClient _client;
    private const string Prefix = "hc_";
    private const string Suffix = ".graphql";

    public AzureBlobStorageOperationDocumentStorageTests(AzureStorageBlobResource blobStorageResource)
    {
        _client = blobStorageResource.CreateBlobServiceClient().GetBlobContainerClient("test");
        _client.CreateIfNotExists();
    }

    [Fact]
    public async Task Write_OperationDocument_To_Storage()
    {
        // arrange
        var documentId = new OperationDocumentId(Guid.NewGuid().ToString("N"));
        var storage = new AzureBlobOperationDocumentStorage(_client, Prefix, Suffix);
        var document = new OperationDocumentSourceText("{ foo }");

        // act
        await storage.SaveAsync(documentId, document);

        // assert
        var actual = await ReadBlob(documentId.Value);
        actual.MatchSnapshot();

        await DeleteBlob(documentId.Value);
    }

    [Fact]
    public async Task Write_OperationDocument_documentId_Invalid()
    {
        // arrange
        var documentId = new OperationDocumentId();
        var storage = new AzureBlobOperationDocumentStorage(_client, Prefix, Suffix);
        var document = new OperationDocumentSourceText("{ foo }");

        // act
        async Task Action() => await storage.SaveAsync(documentId, document);

        // assert
        await Assert.ThrowsAsync<ArgumentNullException>(Action);
    }

    [Fact]
    public async Task Write_OperationDocument_OperationDocument_Is_Null()
    {
        // arrange
        var storage = new AzureBlobOperationDocumentStorage(_client, Prefix, Suffix);
        var documentId = new OperationDocumentId(Guid.NewGuid().ToString("N"));

        // act
        async Task Action() => await storage.SaveAsync(documentId, null!);

        // assert
        await Assert.ThrowsAsync<ArgumentNullException>(Action);
    }

    [Fact]
    public async Task Read_OperationDocument_From_Storage()
    {
        // arrange
        var documentId = new OperationDocumentId(Guid.NewGuid().ToString("N"));
        var storage = new AzureBlobOperationDocumentStorage(_client, Prefix, Suffix);
        var buffer = "{ foo }"u8.ToArray();
        await WriteBlob(documentId.Value, buffer);

        // act
        var document = await storage.TryReadAsync(documentId);

        // assert
        Assert.NotNull(document);
        Assert.IsType<OperationDocument>(document).Document.Print().MatchSnapshot();

        await DeleteBlob(documentId.Value);
    }

    [Fact]
    public async Task Read_OperationDocument_documentId_Invalid()
    {
        // arrange
        var documentId = new OperationDocumentId();
        var storage = new AzureBlobOperationDocumentStorage(_client, Prefix, Suffix);

        // act
        async Task Action() => await storage.TryReadAsync(documentId);

        // assert
        await Assert.ThrowsAsync<ArgumentNullException>(Action);
    }

    private async Task<string> ReadBlob(string key)
    {
        await using var mem = new MemoryStream();
        await using var blob = await _client.GetBlobClient(BlobName(key)).OpenReadAsync();
        await blob.CopyToAsync(mem);
        var value = Utf8GraphQLParser.Parse(mem.ToArray()).Print();
        return value;
    }

    private async Task WriteBlob(string key, byte[] buffer)
    {
        await using var @out = await _client.GetBlobClient(BlobName(key)).OpenWriteAsync(true);
        await @out.WriteAsync(buffer);
        await @out.FlushAsync();
    }

    private async Task DeleteBlob(string key) => await _client.DeleteBlobAsync(BlobName(key));

    private static string BlobName(string key) => $"{Prefix}{key}{Suffix}";
}
