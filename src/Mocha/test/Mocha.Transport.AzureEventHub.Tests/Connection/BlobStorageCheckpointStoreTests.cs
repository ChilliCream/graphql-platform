using Azure.Storage.Blobs;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace Mocha.Transport.AzureEventHub.Tests.Connection;

public sealed class BlobStorageCheckpointStoreTests : IAsyncLifetime
{
    private const string AzuriteConnectionStringTemplate =
        "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;"
        + "AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/"
        + "K1SZFPTOtr/KBHBeksoGMGw==;"
        + "BlobEndpoint=http://127.0.0.1:{0}/devstoreaccount1;";

    private const string ContainerName = "checkpoints";

    private IContainer _azurite = null!;
    private BlobContainerClient _containerClient = null!;

    public async Task InitializeAsync()
    {
        _azurite = new ContainerBuilder("mcr.microsoft.com/azure-storage/azurite")
            .WithPortBinding(10000, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("Azurite"))
            .Build();

        await _azurite.StartAsync();

        var port = _azurite.GetMappedPublicPort(10000);
        var connectionString = string.Format(AzuriteConnectionStringTemplate, port);
        _containerClient = new BlobContainerClient(connectionString, ContainerName);
        await _containerClient.CreateIfNotExistsAsync();
    }

    public async Task DisposeAsync()
    {
        await _azurite.DisposeAsync();
    }

    [Fact]
    public async Task GetCheckpointAsync_Should_ReturnNull_When_NoBlobExists()
    {
        // arrange
        var store = new BlobStorageCheckpointStore(_containerClient);

        // act
        var result = await store.GetCheckpointAsync(
            "test-ns.servicebus.windows.net",
            "my-hub",
            "$Default",
            "0",
            CancellationToken.None);

        // assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SetCheckpointAsync_Should_PersistValue_When_GetCheckpointAsyncCalled()
    {
        // arrange
        var store = new BlobStorageCheckpointStore(_containerClient);

        // act
        await store.SetCheckpointAsync(
            "test-ns.servicebus.windows.net",
            "my-hub",
            "$Default",
            "1",
            42L,
            CancellationToken.None);

        var result = await store.GetCheckpointAsync(
            "test-ns.servicebus.windows.net",
            "my-hub",
            "$Default",
            "1",
            CancellationToken.None);

        // assert
        Assert.Equal(42L, result);
    }

    [Fact]
    public async Task SetCheckpointAsync_Should_OverwriteValue_When_CalledMultipleTimes()
    {
        // arrange
        var store = new BlobStorageCheckpointStore(_containerClient);

        await store.SetCheckpointAsync(
            "test-ns.servicebus.windows.net",
            "my-hub",
            "$Default",
            "2",
            100L,
            CancellationToken.None);

        // act
        await store.SetCheckpointAsync(
            "test-ns.servicebus.windows.net",
            "my-hub",
            "$Default",
            "2",
            200L,
            CancellationToken.None);

        var result = await store.GetCheckpointAsync(
            "test-ns.servicebus.windows.net",
            "my-hub",
            "$Default",
            "2",
            CancellationToken.None);

        // assert
        Assert.Equal(200L, result);
    }

    [Fact]
    public async Task GetCheckpointAsync_Should_IsolateByPartition_When_DifferentPartitionIds()
    {
        // arrange
        var store = new BlobStorageCheckpointStore(_containerClient);

        await store.SetCheckpointAsync(
            "test-ns.servicebus.windows.net",
            "my-hub",
            "$Default",
            "3",
            300L,
            CancellationToken.None);

        await store.SetCheckpointAsync(
            "test-ns.servicebus.windows.net",
            "my-hub",
            "$Default",
            "4",
            400L,
            CancellationToken.None);

        // act
        var result3 = await store.GetCheckpointAsync(
            "test-ns.servicebus.windows.net",
            "my-hub",
            "$Default",
            "3",
            CancellationToken.None);

        var result4 = await store.GetCheckpointAsync(
            "test-ns.servicebus.windows.net",
            "my-hub",
            "$Default",
            "4",
            CancellationToken.None);

        // assert
        Assert.Equal(300L, result3);
        Assert.Equal(400L, result4);
    }

    [Fact]
    public async Task GetCheckpointAsync_Should_IsolateByConsumerGroup_When_DifferentGroups()
    {
        // arrange
        var store = new BlobStorageCheckpointStore(_containerClient);

        await store.SetCheckpointAsync(
            "test-ns.servicebus.windows.net",
            "my-hub",
            "group-a",
            "0",
            500L,
            CancellationToken.None);

        await store.SetCheckpointAsync(
            "test-ns.servicebus.windows.net",
            "my-hub",
            "group-b",
            "0",
            600L,
            CancellationToken.None);

        // act
        var resultA = await store.GetCheckpointAsync(
            "test-ns.servicebus.windows.net",
            "my-hub",
            "group-a",
            "0",
            CancellationToken.None);

        var resultB = await store.GetCheckpointAsync(
            "test-ns.servicebus.windows.net",
            "my-hub",
            "group-b",
            "0",
            CancellationToken.None);

        // assert
        Assert.Equal(500L, resultA);
        Assert.Equal(600L, resultB);
    }
}
