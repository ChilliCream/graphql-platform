using Mocha.Middlewares;

namespace Mocha.Transport.AzureServiceBus.Tests.Behaviors;

public class AzureServiceBusContextExtensionsTests
{
    [Fact]
    public void AzureServiceBus_Should_Throw_When_FeatureNotPresent()
    {
        // arrange
        var context = new ReceiveContext();

        // act & assert
        var ex = Assert.Throws<InvalidOperationException>(() => context.AzureServiceBus());
        Assert.Contains("Azure Service Bus", ex.Message);
    }

    [Fact]
    public void AzureServiceBus_Should_ReturnFeature_When_Present()
    {
        // arrange
        var feature = new StubAzureServiceBusMessageContext();
        var context = new ReceiveContext();
        context.Features.Set<IAzureServiceBusMessageContext>(feature);

        // act
        var resolved = context.AzureServiceBus();

        // assert
        Assert.Same(feature, resolved);
    }

    [Fact]
    public void TryGetAzureServiceBus_Should_ReturnFalse_When_FeatureNotPresent()
    {
        // arrange
        var context = new ReceiveContext();

        // act
        var present = context.TryGetAzureServiceBus(out var feature);

        // assert
        Assert.False(present);
        Assert.Null(feature);
    }

    [Fact]
    public void TryGetAzureServiceBus_Should_ReturnTrueAndFeature_When_Present()
    {
        // arrange
        var stub = new StubAzureServiceBusMessageContext();
        var context = new ReceiveContext();
        context.Features.Set<IAzureServiceBusMessageContext>(stub);

        // act
        var present = context.TryGetAzureServiceBus(out var resolved);

        // assert
        Assert.True(present);
        Assert.Same(stub, resolved);
    }

    private sealed class StubAzureServiceBusMessageContext : IAzureServiceBusMessageContext
    {
        public Azure.Messaging.ServiceBus.ServiceBusReceivedMessage Message
            => throw new NotSupportedException();

        public string EntityPath => "stub";

        public int DeliveryCount => 0;

        public DateTimeOffset LockedUntil => DateTimeOffset.MinValue;

        public Task DeadLetterAsync(
            string reason,
            string? description = null,
            IDictionary<string, object>? properties = null,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task AbandonAsync(
            IDictionary<string, object>? propertiesToModify = null,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
