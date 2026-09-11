using Moq;
using Xunit;

namespace Mocha.Transport.Nats.Tests;

public class NatsEndpointDescriptorTests
{
    private static NatsMessagingTransportDescriptor CreateDescriptor()
        => new(Mock.Of<IMessagingSetupContext>());

    [Fact]
    public void Endpoint_Should_DeriveADurableName_When_GivenAnEndpointName()
    {
        // arrange
        var descriptor = CreateDescriptor();

        // act
        descriptor.Endpoint("order-service.order-created");

        // assert
        var endpoint = Assert.IsType<NatsReceiveEndpointConfiguration>(
            Assert.Single(descriptor.CreateConfiguration().ReceiveEndpoints));

        Assert.Equal("order-service.order-created", endpoint.Name);
        Assert.Equal("order-service_order-created", endpoint.ConsumerName);
    }

    [Fact]
    public void Endpoint_Should_ReturnTheSameDeclaration_When_TheNameRepeats()
    {
        // arrange
        var descriptor = CreateDescriptor();

        // act
        descriptor.Endpoint("orders").Subject("order-service.order-created");
        descriptor.Endpoint("orders").Subject("order-service.order-cancelled");

        // assert
        var endpoint = Assert.IsType<NatsReceiveEndpointConfiguration>(
            Assert.Single(descriptor.CreateConfiguration().ReceiveEndpoints));

        Assert.Equal(
            ["order-service.order-created", "order-service.order-cancelled"],
            endpoint.FilterSubjects);
    }

    [Fact]
    public void FromStream_Should_PinTheEndpointToAStream_When_Declared()
    {
        // arrange
        var descriptor = CreateDescriptor();

        // act
        descriptor.Endpoint("orders").FromStream("ORDER_SERVICE").ConsumerName("orders.worker");

        // assert
        var endpoint = Assert.IsType<NatsReceiveEndpointConfiguration>(
            Assert.Single(descriptor.CreateConfiguration().ReceiveEndpoints));

        Assert.Equal("ORDER_SERVICE", endpoint.StreamName);
        Assert.Equal("orders_worker", endpoint.ConsumerName);
    }
}
