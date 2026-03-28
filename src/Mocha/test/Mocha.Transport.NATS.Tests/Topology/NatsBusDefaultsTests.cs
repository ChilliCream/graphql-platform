namespace Mocha.Transport.NATS.Tests.Topology;

public class NatsBusDefaultsTests
{
    [Fact]
    public void StreamDefaults_Should_ApplyMaxAge_When_ConfigurationDoesNotOverride()
    {
        // arrange
        var defaults = new NatsBusDefaults();
        defaults.Stream.MaxAge = TimeSpan.FromHours(24);
        var config = new NatsStreamConfiguration { Name = "test-stream" };

        // act
        defaults.Stream.ApplyTo(config);

        // assert
        Assert.Equal(TimeSpan.FromHours(24), config.MaxAge);
    }

    [Fact]
    public void StreamDefaults_Should_NotOverrideMaxAge_When_ExplicitlySet()
    {
        // arrange
        var defaults = new NatsBusDefaults();
        defaults.Stream.MaxAge = TimeSpan.FromHours(24);
        var config = new NatsStreamConfiguration { Name = "test-stream", MaxAge = TimeSpan.FromMinutes(5) };

        // act
        defaults.Stream.ApplyTo(config);

        // assert
        Assert.Equal(TimeSpan.FromMinutes(5), config.MaxAge);
    }

    [Fact]
    public void StreamDefaults_Should_ApplyMaxMsgs_When_ConfigurationDoesNotOverride()
    {
        // arrange
        var defaults = new NatsBusDefaults();
        defaults.Stream.MaxMsgs = 10000;
        var config = new NatsStreamConfiguration { Name = "test-stream" };

        // act
        defaults.Stream.ApplyTo(config);

        // assert
        Assert.Equal(10000, config.MaxMsgs);
    }

    [Fact]
    public void StreamDefaults_Should_ApplyMaxBytes_When_ConfigurationDoesNotOverride()
    {
        // arrange
        var defaults = new NatsBusDefaults();
        defaults.Stream.MaxBytes = 1024 * 1024 * 100;
        var config = new NatsStreamConfiguration { Name = "test-stream" };

        // act
        defaults.Stream.ApplyTo(config);

        // assert
        Assert.Equal(1024 * 1024 * 100, config.MaxBytes);
    }

    [Fact]
    public void ConsumerDefaults_Should_ApplyMaxAckPending_When_ConfigurationDoesNotOverride()
    {
        // arrange
        var defaults = new NatsBusDefaults();
        defaults.Consumer.MaxAckPending = 50;
        var config = new NatsConsumerConfiguration { Name = "test-consumer" };

        // act
        defaults.Consumer.ApplyTo(config);

        // assert
        Assert.Equal(50, config.MaxAckPending);
    }

    [Fact]
    public void ConsumerDefaults_Should_NotOverrideMaxAckPending_When_ExplicitlySet()
    {
        // arrange
        var defaults = new NatsBusDefaults();
        defaults.Consumer.MaxAckPending = 50;
        var config = new NatsConsumerConfiguration { Name = "test-consumer", MaxAckPending = 200 };

        // act
        defaults.Consumer.ApplyTo(config);

        // assert
        Assert.Equal(200, config.MaxAckPending);
    }

    [Fact]
    public void ConsumerDefaults_Should_ApplyAckWait_When_ConfigurationDoesNotOverride()
    {
        // arrange
        var defaults = new NatsBusDefaults();
        defaults.Consumer.AckWait = TimeSpan.FromSeconds(60);
        var config = new NatsConsumerConfiguration { Name = "test-consumer" };

        // act
        defaults.Consumer.ApplyTo(config);

        // assert
        Assert.Equal(TimeSpan.FromSeconds(60), config.AckWait);
    }
}
