using System.Reactive.Disposables;
using HotChocolate.Adapters.OpenApi.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Adapters.OpenApi;

public class OpenApiManagerTests
{
    [Fact]
    public async Task Get_Should_NotSubscribeToStorage_When_RegistrationIsCreatedAfterStop()
    {
        // arrange
        var storage = new SubscriberCountingStorage();
        var services = new ServiceCollection();
        services
            .AddGraphQLServer()
            .AddOpenApi()
            .AddOpenApiDefinitionStorage(storage)
            .AddBasicServer();
        await using var serviceProvider = services.BuildServiceProvider();
        var manager = serviceProvider.GetRequiredService<OpenApiManager>();
        await manager.StopAsync(TestContext.Current.CancellationToken);

        // act
        manager.Get();

        // assert
        Assert.Equal(0, storage.SubscribeCallCount);
    }

    [Fact]
    public async Task StopAsync_Should_UnsubscribeEveryRegistration_When_Canceled()
    {
        // arrange
        var storageA = new SubscriberCountingStorage();
        var storageB = new SubscriberCountingStorage();
        var services = new ServiceCollection();
        services
            .AddGraphQLServer("a")
            .AddOpenApi()
            .AddOpenApiDefinitionStorage(storageA)
            .AddBasicServer();
        services
            .AddGraphQLServer("b")
            .AddOpenApi()
            .AddOpenApiDefinitionStorage(storageB)
            .AddBasicServer();
        await using var serviceProvider = services.BuildServiceProvider();
        var manager = serviceProvider.GetRequiredService<OpenApiManager>();
        manager.Get("a");
        manager.Get("b");
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // act
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => manager.StopAsync(cts.Token));

        // assert
        Assert.Equal(0, storageA.SubscriberCount);
        Assert.Equal(0, storageB.SubscriberCount);
    }

    private sealed class SubscriberCountingStorage : IOpenApiDefinitionStorage
    {
        private int _subscribeCallCount;
        private int _subscriberCount;

        public int SubscribeCallCount => Volatile.Read(ref _subscribeCallCount);

        public int SubscriberCount => Volatile.Read(ref _subscriberCount);

        public ValueTask<IEnumerable<IOpenApiDefinition>> GetDefinitionsAsync(
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult<IEnumerable<IOpenApiDefinition>>([]);

        public IDisposable Subscribe(IObserver<OpenApiDefinitionStorageEventArgs> observer)
        {
            Interlocked.Increment(ref _subscribeCallCount);
            Interlocked.Increment(ref _subscriberCount);

            return Disposable.Create(() => Interlocked.Decrement(ref _subscriberCount));
        }
    }
}
