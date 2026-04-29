using Mocha.Middlewares;

namespace Mocha.Transport.AzureServiceBus.Tests.Behaviors;

public class AzureServiceBusContextExtensionsTests
{
    [Fact]
    public void GetAzureServiceBusEventArgs_Should_Throw_When_FeatureNotPresent()
    {
        // arrange
        var context = new ReceiveContext();

        // act & assert
        var ex = Assert.Throws<InvalidOperationException>(() => context.GetAzureServiceBusEventArgs());
        Assert.Contains("Azure Service Bus", ex.Message);
    }
}
