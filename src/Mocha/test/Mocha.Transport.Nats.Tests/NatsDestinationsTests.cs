using Xunit;

namespace Mocha.Transport.Nats.Tests;

public class NatsDestinationsTests
{
    [Fact]
    public void TryResolveExplicit_Should_ReadTheSubject_When_GivenATransportAddress()
    {
        // arrange
        var address = new Uri("nats://localhost:4222/ORDER_SERVICE/s/order-service.order-created");

        // act
        var resolved = NatsDestinations.TryResolveExplicit("nats", address, out var subject);

        // assert
        Assert.True(resolved);
        Assert.Equal("order-service.order-created", subject);
    }

    [Fact]
    public void TryResolveExplicit_Should_ReadTheSubject_When_GivenABareSubjectScheme()
    {
        // arrange
        var address = new Uri("subject:Order-Service.OrderCreated");

        // act
        var resolved = NatsDestinations.TryResolveExplicit("nats", address, out var subject);

        // assert
        Assert.True(resolved);
        Assert.Equal("Order-Service.OrderCreated", subject);
    }

    [Fact]
    public void TryResolveExplicit_Should_Fail_When_TheAuthorityFormWouldLoseSubjectCase()
    {
        // arrange
        // Uri lower-cases the authority, so this form cannot carry a case-sensitive subject.
        var address = new Uri("subject://Order-Service.OrderCreated");

        // act
        var resolved = NatsDestinations.TryResolveExplicit("nats", address, out _);

        // assert
        Assert.Equal("order-service.ordercreated", address.Host);
        Assert.False(resolved);
    }

    [Fact]
    public void TryResolveExplicit_Should_ReadTheSubject_When_GivenASchemaRelativeAddress()
    {
        // arrange
        var address = new Uri("nats:///s/order-service.order-created");

        // act
        var resolved = NatsDestinations.TryResolveExplicit("nats", address, out var subject);

        // assert
        Assert.True(resolved);
        Assert.Equal("order-service.order-created", subject);
    }

    [Fact]
    public void TryResolveExplicit_Should_Fail_When_GivenAConsumerAddress()
    {
        // arrange
        var address = new Uri("nats://localhost:4222/ORDER_SERVICE/c/order-service_order-created");

        // act and assert
        Assert.False(NatsDestinations.TryResolveExplicit("nats", address, out _));
    }

    [Fact]
    public void TryResolveExplicit_Should_Fail_When_GivenAnUnrelatedScheme()
    {
        // arrange
        var address = new Uri("amqp://localhost/e/order-created");

        // act and assert
        Assert.False(NatsDestinations.TryResolveExplicit("nats", address, out _));
    }
}
