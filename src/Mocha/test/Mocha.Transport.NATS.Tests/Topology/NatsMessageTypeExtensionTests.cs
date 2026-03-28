namespace Mocha.Transport.NATS.Tests.Topology;

public class NatsMessageTypeExtensionTests
{
    [Fact]
    public void ToNatsSubject_Should_CreateCorrectUri_When_DefaultSchema()
    {
        // act
        var uri = new Uri($"{NatsTransportConfiguration.DefaultSchema}:s/orders.created");

        // assert
        Assert.Equal("nats", uri.Scheme);
        Assert.Equal("s/orders.created", uri.AbsolutePath.TrimStart('/'));
    }

    [Fact]
    public void ToNatsSubject_Should_CreateCorrectUri_When_CustomSchema()
    {
        // act
        var uri = new Uri("custom-nats:s/orders.created");

        // assert
        Assert.Equal("custom-nats", uri.Scheme);
        Assert.Equal("s/orders.created", uri.AbsolutePath.TrimStart('/'));
    }

    [Fact]
    public void SubjectShorthand_Should_ParseHost_When_SubjectSchemeUsed()
    {
        // arrange
        var uri = new Uri("subject://my-subject");

        // assert
        Assert.Equal("subject", uri.Scheme);
        Assert.Equal("my-subject", uri.Host);
    }
}
