using Xunit;

namespace Mocha.Transport.Nats.Tests;

public class NatsConnectionProviderTests
{
    [Theory]
    [InlineData("nats://localhost:4222", "localhost", 4222)]
    [InlineData("nats://nats.internal:4333", "nats.internal", 4333)]
    [InlineData("localhost:4222", "localhost", 4222)]
    [InlineData("nats://localhost", "localhost", NatsConnectionProvider.DefaultPort)]
    public void ParseFirstServer_Should_ReadHostAndPort_When_GivenACommonUrlShape(
        string url,
        string expectedHost,
        int expectedPort)
    {
        // act
        var (host, port) = NatsConnectionProvider.ParseFirstServer(url);

        // assert
        Assert.Equal(expectedHost, host);
        Assert.Equal(expectedPort, port);
    }

    [Fact]
    public void ParseFirstServer_Should_UseOnlyTheFirstEntry_When_GivenACluster()
    {
        // act
        var (host, port) = NatsConnectionProvider.ParseFirstServer(
            "nats://first:4222, nats://second:4223, nats://third:4224");

        // assert
        Assert.Equal("first", host);
        Assert.Equal(4222, port);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ParseFirstServer_Should_Throw_When_TheUrlIsEmpty(string url)
    {
        // act and assert
        Assert.Throws<ArgumentException>(() => NatsConnectionProvider.ParseFirstServer(url));
    }
}
