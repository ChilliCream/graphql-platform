using Xunit;

namespace Mocha.Transport.Nats.Tests;

public class NatsServerCapabilitiesTests
{
    [Theory]
    [InlineData("2.10.5", false, false)]
    [InlineData("2.11.0", true, false)]
    [InlineData("2.12.1", true, true)]
    [InlineData("v2.12.0-RC.3", true, true)]
    public void FromServerVersion_Should_Gate_Features_When_The_Server_Reports_A_Version(
        string version,
        bool ttl,
        bool schedules)
    {
        // act
        var capabilities = NatsServerCapabilities.FromServerVersion(version);

        // assert
        Assert.Equal(ttl, capabilities.SupportsMessageTtl);
        Assert.Equal(schedules, capabilities.SupportsMessageSchedules);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-version")]
    public void FromServerVersion_Should_Assume_Capable_When_The_Version_Is_Unreadable(string? version)
    {
        // act
        var capabilities = NatsServerCapabilities.FromServerVersion(version);

        // assert
        Assert.Null(capabilities.Version);
        Assert.True(capabilities.SupportsMessageTtl);
        Assert.True(capabilities.SupportsMessageSchedules);
    }
}
