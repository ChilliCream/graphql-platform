using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging.Abstractions;

namespace Mocha.Transport.AzureServiceBus.Tests;

public class QueueHeartbeatTests
{
    private const string FakeConnectionString =
        "Endpoint=sb://fake.servicebus.windows.net/;SharedAccessKeyName=k;SharedAccessKey=a2V5";

    [Fact]
    public async Task Constructor_Should_CreateInstance_When_IntervalProvided()
    {
        // arrange
        await using var client = new ServiceBusClient(FakeConnectionString);
        var receiver = client.CreateReceiver("test-queue");

        // act
        var heartbeat = new QueueHeartbeat(
            receiver,
            TimeSpan.FromMinutes(5),
            NullLogger.Instance,
            "test-queue");

        // assert
        Assert.NotNull(heartbeat);

        // cleanup
        await heartbeat.DisposeAsync();
    }

    [Fact]
    public async Task Constructor_Should_CreateInstance_When_UsingDefaultInterval()
    {
        // arrange
        await using var client = new ServiceBusClient(FakeConnectionString);
        var receiver = client.CreateReceiver("test-queue");

        // act
        var heartbeat = new QueueHeartbeat(
            receiver,
            NullLogger.Instance,
            "test-queue");

        // assert
        Assert.NotNull(heartbeat);

        // cleanup
        await heartbeat.DisposeAsync();
    }

    [Fact]
    public async Task DisposeAsync_Should_Complete_When_Called()
    {
        // arrange
        await using var client = new ServiceBusClient(FakeConnectionString);
        var receiver = client.CreateReceiver("test-queue");
        var heartbeat = new QueueHeartbeat(
            receiver,
            TimeSpan.FromMinutes(5),
            NullLogger.Instance,
            "test-queue");

        // act & assert — should not throw
        await heartbeat.DisposeAsync();
    }
}
