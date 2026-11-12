using System.Reactive.Disposables;
using HotChocolate.Adapters.OpenApi.Storage;
using HotChocolate.Execution;
using HotChocolate.Language;
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
    public async Task StopAsync_Should_DiscardUpdateInProgress_When_Canceled()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var storage = new BlockingStorage();
        var services = new ServiceCollection();
        services
            .AddGraphQLServer()
            .AddOpenApi()
            .AddOpenApiDefinitionStorage(storage)
            .AddBasicServer();
        await using var serviceProvider = services.BuildServiceProvider();
        var manager = serviceProvider.GetRequiredService<OpenApiManager>();
        var registration = manager.Get();
        var executor = await serviceProvider.GetRequestExecutorAsync(
            cancellationToken: cancellationToken);
        storage.BlockReads(
            OpenApiDefinitionParser.Parse(
                Utf8GraphQLParser.Parse(
                    """
                    query GetUsers @http(method: GET, route: "/users") {
                      usersWithoutAuth {
                        id
                      }
                    }
                    """)));
        var update = registration.Registry.UpdateSchemaAsync(executor.Schema, cancellationToken);
        await storage.ReadStarted.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => manager.StopAsync(cts.Token));

        // act
        storage.ReleaseReads();
        await update;

        // assert
        Assert.Empty(registration.EndpointDataSource.Endpoints);
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

    private sealed class BlockingStorage : IOpenApiDefinitionStorage
    {
        private readonly TaskCompletionSource _readStarted =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _readsReleased =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private IOpenApiDefinition[] _definitions = [];
        private bool _blocking;

        public Task ReadStarted => _readStarted.Task;

        public void BlockReads(params IOpenApiDefinition[] definitions)
        {
            _definitions = definitions;
            _blocking = true;
        }

        public void ReleaseReads() => _readsReleased.TrySetResult();

        public async ValueTask<IEnumerable<IOpenApiDefinition>> GetDefinitionsAsync(
            CancellationToken cancellationToken = default)
        {
            if (_blocking)
            {
                _readStarted.TrySetResult();
                await _readsReleased.Task;
            }

            return _definitions;
        }

        public IDisposable Subscribe(IObserver<OpenApiDefinitionStorageEventArgs> observer)
            => Disposable.Empty;
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
