namespace Mocha.Transport.AzureServiceBus.Tests;

public class AzureServiceBusMessageHeadersTests
{
    [Theory]
    [InlineData(AzureServiceBusMessageHeaders.SentAt)]
    [InlineData(AzureServiceBusMessageHeaders.SessionId)]
    [InlineData(AzureServiceBusMessageHeaders.PartitionKey)]
    [InlineData(AzureServiceBusMessageHeaders.ReplyToSessionId)]
    [InlineData(AzureServiceBusMessageHeaders.To)]
    [InlineData("x-conversation-id")]
    [InlineData("x-causation-id")]
    [InlineData("x-source-address")]
    [InlineData("x-destination-address")]
    [InlineData("x-fault-address")]
    [InlineData("x-message-type")]
    [InlineData("x-enclosed-message-types")]
    public void IsFrameworkHeader_Should_ReturnTrue_When_KeyIsAKnownFrameworkHeader(string key)
    {
        // act
        var result = AzureServiceBusMessageHeaders.IsFrameworkHeader(key);

        // assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("x-mocha-custom")]
    [InlineData("x-sent-at-custom")]
    [InlineData("x-session-id-suffix")]
    [InlineData("x-to-2")]
    [InlineData("x-conversation-identifier")]
    public void IsFrameworkHeader_Should_ReturnFalse_When_KeyOnlySharesAPrefixWithAFrameworkHeader(string key)
    {
        // act
        var result = AzureServiceBusMessageHeaders.IsFrameworkHeader(key);

        // assert
        Assert.False(result);
    }
}
