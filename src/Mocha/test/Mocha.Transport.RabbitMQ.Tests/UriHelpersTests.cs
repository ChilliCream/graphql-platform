namespace Mocha.Transport.RabbitMQ.Tests;

public class UriHelpersTests
{
    [Fact]
    public void TryGetRoutingKey_Should_ReturnTrue_When_RoutingKeyPresent()
    {
        // arrange
        var uri = new Uri("rabbitmq:///e/my-exchange?routingKey=order.created");

        // act
        var result = uri.TryGetRoutingKey(out var routingKey);

        // assert
        Assert.True(result);
        Assert.Equal("order.created", routingKey);
    }

    [Fact]
    public void TryGetRoutingKey_Should_ReturnFalse_When_NoQueryString()
    {
        // arrange
        var uri = new Uri("rabbitmq:///e/my-exchange");

        // act
        var result = uri.TryGetRoutingKey(out var routingKey);

        // assert
        Assert.False(result);
        Assert.Null(routingKey);
    }

    [Fact]
    public void TryGetRoutingKey_Should_ReturnFalse_When_DifferentQueryParam()
    {
        // arrange
        var uri = new Uri("rabbitmq:///e/my-exchange?other=value");

        // act
        var result = uri.TryGetRoutingKey(out var routingKey);

        // assert
        Assert.False(result);
        Assert.Null(routingKey);
    }

    [Fact]
    public void TryGetRoutingKey_Should_DecodeValue_When_UrlEncoded()
    {
        // arrange
        var uri = new Uri("rabbitmq:///e/my-exchange?routingKey=order%2Ecreated%2B1");

        // act
        var result = uri.TryGetRoutingKey(out var routingKey);

        // assert
        Assert.True(result);
        Assert.Equal("order.created+1", routingKey);
    }
}
