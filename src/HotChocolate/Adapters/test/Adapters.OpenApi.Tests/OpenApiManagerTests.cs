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
        Assert.Equal(0, storage.SubscriberCount);
    }

    private sealed class SubscriberCountingStorage : IOpenApiDefinitionStorage
    {
        private int _subscriberCount;

        public int SubscriberCount => Volatile.Read(ref _subscriberCount);

        public ValueTask<IEnumerable<IOpenApiDefinition>> GetDefinitionsAsync(
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult<IEnumerable<IOpenApiDefinition>>([]);

        public IDisposable Subscribe(IObserver<OpenApiDefinitionStorageEventArgs> observer)
        {
            Interlocked.Increment(ref _subscriberCount);

            return Disposable.Create(() => Interlocked.Decrement(ref _subscriberCount));
        }
    }
}
