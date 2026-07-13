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
}
