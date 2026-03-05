using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.InMemory;

namespace Mocha.Tests;

public class EndpointRouterTests
{
    [Fact]
    public void GetOrCreate_Should_ReturnCachedEndpoint_When_AddressAlreadyKnown()
    {
        // arrange
        var runtime = CreateRuntime();
        var endpoints = runtime.Endpoints;
        var existingEndpoint = endpoints.Endpoints.First();

        // act
        var result = endpoints.GetOrCreate(runtime, existingEndpoint.Address);

        // assert
        Assert.Same(existingEndpoint, result);
    }

    [Fact]
    public void GetOrCreate_Should_CreateAndRegisterEndpoint_When_AddressNotKnown()
    {
        // arrange
        var runtime = CreateRuntime();
        var endpoints = runtime.Endpoints;
        var address = new Uri("queue:new-queue");

        // act
        var result = endpoints.GetOrCreate(runtime, address);

        // assert
        Assert.NotNull(result);
        Assert.True(result.IsCompleted);
        Assert.True(endpoints.TryGet(address, out var found));
        Assert.Same(result, found);
    }

    [Fact]
    public void GetOrCreate_Should_ThrowInvalidOperationException_When_NoTransportCanHandleAddress()
    {
        // arrange
        var runtime = CreateRuntime();
        var unknownAddress = new Uri("ftp://unknown-host/path");

        // act & assert
        Assert.Throws<InvalidOperationException>(() => runtime.Endpoints.GetOrCreate(runtime, unknownAddress));
    }

    [Fact]
    public async Task GetOrCreate_Should_ReturnSameEndpoint_When_ConcurrentCallsWithSameAddress()
    {
        // arrange
        var runtime = CreateRuntime();
        var endpoints = runtime.Endpoints;
        var address = new Uri("queue:concurrent-test");
        var results = new DispatchEndpoint[10];

        // act
        var tasks = Enumerable
            .Range(0, 10)
            .Select(i => Task.Run(() => results[i] = endpoints.GetOrCreate(runtime, address)))
            .ToArray();
        await Task.WhenAll(tasks);

        // assert - all return same instance
        Assert.All(results, r => Assert.Same(results[0], r));
    }

    [Fact]
    public void AddAddress_Should_MakeEndpointFindableByAlias()
    {
        // arrange
        var runtime = CreateRuntime();
        var endpoints = runtime.Endpoints;
        var endpoint = endpoints.GetOrCreate(runtime, new Uri("queue:primary"));
        var alias = new Uri("queue:alias");

        // act
        endpoints.AddAddress(endpoint, alias);

        // assert
        Assert.True(endpoints.TryGet(alias, out var found));
        Assert.Same(endpoint, found);
    }

    [Fact]
    public void AddAddress_Should_ThrowInvalidOperationException_When_EndpointNotRegistered()
    {
        // arrange
        var runtime = CreateRuntime();
        var endpoints = runtime.Endpoints;
        var endpoint = endpoints.GetOrCreate(runtime, new Uri("queue:temp"));
        endpoints.Remove(endpoint);

        // act & assert
        Assert.Throws<InvalidOperationException>(() => endpoints.AddAddress(endpoint, new Uri("queue:alias")));
    }

    [Fact]
    public void Remove_Should_RemoveEndpointAndAllItsAddresses()
    {
        // arrange
        var runtime = CreateRuntime();
        var endpoints = runtime.Endpoints;
        var primary = new Uri("queue:to-remove");
        var alias = new Uri("queue:to-remove-alias");
        var endpoint = endpoints.GetOrCreate(runtime, primary);
        endpoints.AddAddress(endpoint, alias);

        // act
        endpoints.Remove(endpoint);

        // assert
        Assert.DoesNotContain(endpoint, endpoints.Endpoints);
        Assert.False(endpoints.TryGet(primary, out _));
        Assert.False(endpoints.TryGet(alias, out _));
    }

    [Fact]
    public void Remove_Should_PreserveOtherEndpoints_When_SharedAddressExists()
    {
        // arrange
        var runtime = CreateRuntime();
        var endpoints = runtime.Endpoints;
        var ep1 = endpoints.GetOrCreate(runtime, new Uri("queue:ep1"));
        var ep2 = endpoints.GetOrCreate(runtime, new Uri("queue:ep2"));
        var shared = new Uri("queue:shared");
        endpoints.AddAddress(ep1, shared);
        endpoints.AddAddress(ep2, shared);

        // act
        endpoints.Remove(ep1);

        // assert - ep2 still findable via shared address
        Assert.True(endpoints.TryGet(shared, out var found));
        Assert.Same(ep2, found);
    }

    [Fact]
    public void GetAll_Should_ReturnAllEndpoints_When_MultipleEndpointsShareAddress()
    {
        // arrange
        var runtime = CreateRuntime();
        var endpoints = runtime.Endpoints;
        var ep1 = endpoints.GetOrCreate(runtime, new Uri("queue:ep1"));
        var ep2 = endpoints.GetOrCreate(runtime, new Uri("queue:ep2"));
        var shared = new Uri("queue:shared");
        endpoints.AddAddress(ep1, shared);
        endpoints.AddAddress(ep2, shared);

        // act
        var result = endpoints.GetAll(shared);

        // assert
        Assert.Equal(2, result.Count);
        Assert.Contains(ep1, result);
        Assert.Contains(ep2, result);
    }

    [Fact]
    public void GetAll_Should_ReturnEmpty_When_AddressNotKnown()
    {
        // arrange
        var runtime = CreateRuntime();

        // act
        var result = runtime.Endpoints.GetAll(new Uri("queue:nonexistent"));

        // assert
        Assert.True(result.IsEmpty);
    }

    [Fact]
    public void AddOrUpdate_Should_PreserveManuallyAddedAddresses()
    {
        // arrange
        var runtime = CreateRuntime();
        var endpoints = runtime.Endpoints;
        var endpoint = endpoints.GetOrCreate(runtime, new Uri("queue:primary"));
        var alias = new Uri("queue:manual-alias");
        endpoints.AddAddress(endpoint, alias);

        // act
        endpoints.AddOrUpdate(endpoint);

        // assert - alias preserved
        Assert.True(endpoints.TryGet(alias, out var found));
        Assert.Same(endpoint, found);
    }

    [Fact]
    public async Task ConcurrentReadWrite_Should_NotCorruptState()
    {
        // arrange
        var runtime = CreateRuntime();
        var endpoints = runtime.Endpoints;
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        // act - concurrent reads and writes
        var readTask = Task.Run(() =>
        {
            while (!cts.IsCancellationRequested)
            {
                _ = endpoints.Endpoints;
                endpoints.TryGet(new Uri("queue:test"), out _);
                _ = endpoints.GetAll(new Uri("queue:test"));
            }
        }, default);

        var writeTask = Task.Run(() =>
        {
            var i = 0;
            while (!cts.IsCancellationRequested)
            {
                var ep = endpoints.GetOrCreate(runtime, new Uri($"queue:concurrent-{i++}"));
                endpoints.Remove(ep);
            }
        }, default);

        // assert - no exceptions
        await Task.WhenAll([readTask, writeTask]);
    }

    private static MessagingRuntime CreateRuntime()
    {
        var services = new ServiceCollection();
        services.AddMessageBus().AddEventHandler<TestEventHandler>().AddInMemory();
        return (MessagingRuntime)services.BuildServiceProvider().GetRequiredService<IMessagingRuntime>();
    }

    private sealed class TestEvent;

    private sealed class TestEventHandler : IEventHandler<TestEvent>
    {
        public ValueTask HandleAsync(TestEvent message, CancellationToken ct) => default;
    }
}
