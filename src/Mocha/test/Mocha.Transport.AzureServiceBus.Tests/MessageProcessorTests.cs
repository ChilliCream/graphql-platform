using Azure.Messaging.ServiceBus;

namespace Mocha.Transport.AzureServiceBus.Tests;

public class MessageProcessorTests
{
    private const string FakeConnectionString =
        "Endpoint=sb://fake.servicebus.windows.net/;SharedAccessKeyName=k;SharedAccessKey=a2V5";

    [Fact]
    public async Task ForProcessor_Should_ReturnMessageProcessor_When_GivenServiceBusProcessor()
    {
        // arrange
        await using var client = new ServiceBusClient(FakeConnectionString);
        var processor = client.CreateProcessor("test-queue");

        // act
        var messageProcessor = MessageProcessor.ForProcessor(processor);

        // assert
        Assert.NotNull(messageProcessor);
    }

    [Fact]
    public async Task ForSessionProcessor_Should_ReturnMessageProcessor_When_GivenServiceBusSessionProcessor()
    {
        // arrange
        await using var client = new ServiceBusClient(FakeConnectionString);
        var sessionProcessor = client.CreateSessionProcessor("test-queue");

        // act
        var messageProcessor = MessageProcessor.ForSessionProcessor(sessionProcessor);

        // assert
        Assert.NotNull(messageProcessor);
    }

    [Fact]
    public async Task DisposeAsync_Should_DelegateToUnderlyingProcessor_When_CreatedFromProcessor()
    {
        // arrange
        await using var client = new ServiceBusClient(FakeConnectionString);
        var processor = client.CreateProcessor("test-queue");
        var messageProcessor = MessageProcessor.ForProcessor(processor);

        // act & assert — should not throw
        await messageProcessor.DisposeAsync();
    }

    [Fact]
    public async Task DisposeAsync_Should_DelegateToUnderlyingProcessor_When_CreatedFromSessionProcessor()
    {
        // arrange
        await using var client = new ServiceBusClient(FakeConnectionString);
        var sessionProcessor = client.CreateSessionProcessor("test-queue");
        var messageProcessor = MessageProcessor.ForSessionProcessor(sessionProcessor);

        // act & assert — should not throw
        await messageProcessor.DisposeAsync();
    }
}
