using Xunit;

namespace Mocha.Transport.Nats.Tests;

public class NatsAddressTests
{
    private static readonly Uri s_baseAddress = new("nats://localhost:4222/");

    [Fact]
    public void ForSubject_Should_BuildASubjectAddress_When_GivenAStreamAndSubject()
    {
        // act
        var address = NatsAddress.ForSubject(s_baseAddress, "ORDER_SERVICE", "order-service.order-created");

        // assert
        Assert.Equal("nats://localhost:4222/ORDER_SERVICE/s/order-service.order-created", address.ToString());
    }

    [Fact]
    public void ForConsumer_Should_BuildAConsumerAddress_When_GivenAStreamAndDurable()
    {
        // act
        var address = NatsAddress.ForConsumer(s_baseAddress, "ORDER_SERVICE", "order-service_order-created");

        // assert
        Assert.Equal("nats://localhost:4222/ORDER_SERVICE/c/order-service_order-created", address.ToString());
    }

    [Fact]
    public void TryParse_Should_RoundTrip_When_GivenASubjectAddress()
    {
        // arrange
        var address = NatsAddress.ForSubject(s_baseAddress, "ORDER_SERVICE", "order-service.order-created");

        // act
        var parsed = NatsAddress.TryParse(address, out var stream, out var kind, out var name);

        // assert
        Assert.True(parsed);
        Assert.Equal("ORDER_SERVICE", stream);
        Assert.Equal(NatsAddress.SubjectSegment, kind);
        Assert.Equal("order-service.order-created", name);
    }

    [Fact]
    public void TryParse_Should_RoundTrip_When_GivenAConsumerAddress()
    {
        // arrange
        var address = NatsAddress.ForConsumer(s_baseAddress, "ORDER_SERVICE", "order-service_order-created");

        // act
        var parsed = NatsAddress.TryParse(address, out var stream, out var kind, out var name);

        // assert
        Assert.True(parsed);
        Assert.Equal("ORDER_SERVICE", stream);
        Assert.Equal(NatsAddress.ConsumerSegment, kind);
        Assert.Equal("order-service_order-created", name);
    }

    [Theory]
    [InlineData("nats://localhost:4222/ORDER_SERVICE")]
    [InlineData("nats://localhost:4222/ORDER_SERVICE/x/order-created")]
    [InlineData("nats://localhost:4222/ORDER_SERVICE/s/a/b")]
    public void TryParse_Should_Fail_When_TheAddressIsMalformed(string address)
    {
        // act and assert
        Assert.False(NatsAddress.TryParse(new Uri(address), out _, out _, out _));
    }

    [Fact]
    public void TryParse_Should_Fail_When_TheAddressIsNull()
    {
        // act and assert
        Assert.False(NatsAddress.TryParse(null, out _, out _, out _));
    }
}
