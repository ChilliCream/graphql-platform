using Xunit;

namespace Mocha.Transport.Nats.Tests;

public class NatsNamingTests
{
    [Theory]
    [InlineData("order-service", "ORDER_SERVICE")]
    [InlineData("order.service", "ORDER_SERVICE")]
    [InlineData("OrderService", "ORDERSERVICE")]
    public void ToStreamName_Should_UpperSnakeCase_When_GivenAName(
        string serviceName,
        string expected)
    {
        // act and assert
        // Note that "order-service" and "order.service" collide, so two services can derive one
        // stream name.
        Assert.Equal(expected, NatsNaming.ToStreamName(serviceName));
    }

    [Theory]
    [InlineData("order-service.order-created", "order-service_order-created")]
    [InlineData("order-created_error", "order-created_error")]
    [InlineData("order-processing_dead-letter", "order-processing_dead-letter")]
    public void ToDurableName_Should_ReplaceDots_When_GivenAnEndpointName(
        string endpointName,
        string expected)
    {
        // act and assert
        Assert.Equal(expected, NatsNaming.ToDurableName(endpointName));
    }

    [Theory]
    [InlineData("order>created", "order_created")]
    [InlineData("order/created", "order_created")]
    [InlineData("order created", "order_created")]
    [InlineData("order>/created", "order__created")]
    public void ToDurableName_Should_ReplaceEveryIllegalCharacter_When_GivenAnEndpointName(
        string endpointName,
        string expected)
    {
        // act and assert
        Assert.Equal(expected, NatsNaming.ToDurableName(endpointName));
    }

    [Fact]
    public void IsValidName_Should_AcceptDerivedNames_When_TheyComeFromTheConventions()
    {
        // act and assert
        Assert.True(NatsNaming.IsValidName(NatsNaming.ToStreamName("order-service")));
        Assert.True(NatsNaming.IsValidName(NatsNaming.ToDurableName("order-service.order-created")));
    }

    [Theory]
    [InlineData("order-service", true)]
    [InlineData("order_service", true)]
    [InlineData("order.service", false)]
    [InlineData("order service", false)]
    [InlineData("order*", false)]
    [InlineData("order>", false)]
    [InlineData("order/service", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValidName_Should_RejectCharacters_When_NatsForbidsThem(string? name, bool expected)
    {
        // act and assert
        Assert.Equal(expected, NatsNaming.IsValidName(name));
    }

    [Theory]
    [InlineData("order-service.order-created", true)]
    [InlineData("order-service.>", true)]
    [InlineData("order-service.*.created", true)]
    [InlineData("order-service..created", false)]
    [InlineData("order-service.>.created", false)]
    [InlineData("order service.created", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValidSubject_Should_AllowWildcards_When_TokensAreNonEmpty(
        string? subject,
        bool expected)
    {
        // act and assert
        Assert.Equal(expected, NatsNaming.IsValidSubject(subject));
    }

    [Fact]
    public void ToDurableName_Should_ExceedTheRecommendedLength_When_TheNameIsServicePrefixed()
    {
        // act
        // The transport warns about this at start-up rather than shortening the name itself.
        var durable = NatsNaming.ToDurableName("order-management-service.order-line-item-created");

        // assert
        Assert.True(durable.Length > NatsNaming.RecommendedMaxNameLength);
    }
}
