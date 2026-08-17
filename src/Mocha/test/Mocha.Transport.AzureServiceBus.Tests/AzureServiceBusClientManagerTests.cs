using System.Collections.Concurrent;
using Azure.Messaging.ServiceBus;

namespace Mocha.Transport.AzureServiceBus.Tests;

public sealed class AzureServiceBusClientManagerTests
{
    [Fact]
    public async Task InvalidateSenderAsync_Should_DisposeRetiredSender_When_LastLeaseReleased()
    {
        var configuration = new AzureServiceBusTransportConfiguration
        {
            ConnectionString =
                "Endpoint=sb://localhost/;SharedAccessKeyName=test;SharedAccessKey=test"
        };
        await using var manager = new AzureServiceBusClientManager(configuration);
        var lease = manager.AcquireSender("orders");
        var oldSender = lease.Sender;

        var disposalTask = manager.InvalidateSenderAsync("orders", lease.Entry);
        using var replacementLease = manager.AcquireSender("orders");

        Assert.False(disposalTask.IsCompleted);
        Assert.NotSame(oldSender, replacementLease.Sender);

        lease.Dispose();
        await disposalTask;

        Assert.True(oldSender.IsClosed);
        Assert.False(replacementLease.Sender.IsClosed);
    }

    [Fact]
    public async Task AcquireSender_Should_NotLeakUntrackedSenders_When_DisposedConcurrently()
    {
        // arrange
        var configuration = new AzureServiceBusTransportConfiguration
        {
            ConnectionString =
                "Endpoint=sb://localhost/;SharedAccessKeyName=test;SharedAccessKey=test"
        };
        var manager = new AzureServiceBusClientManager(configuration);
        var senders = new ConcurrentBag<ServiceBusSender>();
        var entityPaths = new[] { "orders", "payments", "shipments", "notifications" };
        var senderAcquired = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var workers = Enumerable.Range(0, 8)
            .Select(
                workerIndex => Task.Run(
                    () => AcquireSendersUntilDisposedAsync(workerIndex),
                    TestContext.Current.CancellationToken))
            .ToArray();

        // act
        await senderAcquired.Task.WaitAsync(
            TimeSpan.FromSeconds(10),
            TestContext.Current.CancellationToken);
        await manager.DisposeAsync();
        await Task.WhenAll(workers);

        // assert
        Assert.NotEmpty(senders);
        Assert.All(senders, static sender => Assert.True(sender.IsClosed));
        Assert.Throws<ObjectDisposedException>(() => manager.AcquireSender("orders"));

        async Task AcquireSendersUntilDisposedAsync(int workerIndex)
        {
            var iteration = workerIndex;

            while (true)
            {
                try
                {
                    using var lease = manager.AcquireSender(entityPaths[iteration % entityPaths.Length]);
                    senders.Add(lease.Sender);
                    senderAcquired.TrySetResult();
                }
                catch (ObjectDisposedException)
                {
                    return;
                }

                iteration++;
                await Task.Yield();
            }
        }
    }

    [Fact]
    public async Task DisposeAsync_Should_BeIdempotent_When_CalledMultipleTimes()
    {
        // arrange
        var configuration = new AzureServiceBusTransportConfiguration
        {
            ConnectionString =
                "Endpoint=sb://localhost/;SharedAccessKeyName=test;SharedAccessKey=test"
        };
        var manager = new AzureServiceBusClientManager(configuration);
        ServiceBusSender sender;

        using (var lease = manager.AcquireSender("orders"))
        {
            sender = lease.Sender;
        }

        // act
        await manager.DisposeAsync();
        await manager.DisposeAsync();
        await manager.DisposeAsync();

        // assert
        Assert.True(sender.IsClosed);
        Assert.Throws<ObjectDisposedException>(() => manager.AcquireSender("orders"));
    }

    [Fact]
    public async Task AcquireSender_Should_ThrowObjectDisposedException_When_ManagerAlreadyDisposed()
    {
        // arrange
        var configuration = new AzureServiceBusTransportConfiguration
        {
            ConnectionString =
                "Endpoint=sb://localhost/;SharedAccessKeyName=test;SharedAccessKey=test"
        };
        var manager = new AzureServiceBusClientManager(configuration);
        await manager.DisposeAsync();

        // act
        var exception = Assert.Throws<ObjectDisposedException>(() => manager.AcquireSender("orders"));

        // assert
        Assert.Equal(typeof(AzureServiceBusClientManager).FullName, exception.ObjectName);
    }
}
