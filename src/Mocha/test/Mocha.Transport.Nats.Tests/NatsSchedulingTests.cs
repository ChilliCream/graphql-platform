using Xunit;

namespace Mocha.Transport.Nats.Tests;

public class NatsSchedulingTests
{
    [Fact]
    public void ToSchedulingSubject_Should_DifferFromTheTarget_When_GivenASubject()
    {
        // arrange
        // The server refuses a schedule whose target is the subject it arrived on.
        const string target = "order-service.order-created";

        // act
        var scheduling = NatsScheduling.ToSchedulingSubject(target, "abc123");

        // assert
        Assert.Equal("order-service.order-created._schedule.abc123", scheduling);
        Assert.True(NatsNaming.IsValidSubject(scheduling));
    }

    [Fact]
    public void ToSchedulingSubject_Should_DifferPerMessage_When_TargetingTheSameSubject()
    {
        // arrange
        // A subject holds at most one schedule, so two messages aimed at one target must not share a
        // scheduling subject or the second silently replaces the first.
        const string target = "order-service.order-created";

        // act
        var first = NatsScheduling.ToSchedulingSubject(target, NatsScheduling.NewScheduleId());
        var second = NatsScheduling.ToSchedulingSubject(target, NatsScheduling.NewScheduleId());

        // assert
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void ToSchedulingFilter_Should_CaptureEveryScheduleForTheTarget_When_GivenASubject()
    {
        // arrange
        const string target = "order-service.order-created";

        // act
        var filter = NatsScheduling.ToSchedulingFilter(target);

        // assert
        Assert.Equal("order-service.order-created._schedule.>", filter);

        Assert.True(SubjectMatcher.Matches(
            filter,
            NatsScheduling.ToSchedulingSubject(target, NatsScheduling.NewScheduleId())));
    }

    [Fact]
    public void ToScheduleValue_Should_UseTheAtFormInUtc_When_GivenALocalInstant()
    {
        // arrange
        var deliverAt = new DateTimeOffset(2026, 8, 11, 11, 0, 0, TimeSpan.FromHours(2));

        // act
        var value = NatsScheduling.ToScheduleValue(deliverAt);

        // assert
        Assert.Equal("@at 2026-08-11T09:00:00.0000000Z", value);
    }

    [Theory]
    [InlineData(30, "30s")]
    [InlineData(90, "90s")]
    public void ToTtlValue_Should_FormatAsSeconds_When_GivenADuration(int seconds, string expected)
    {
        // act and assert
        Assert.Equal(expected, NatsScheduling.ToTtlValue(TimeSpan.FromSeconds(seconds)));
    }

    [Fact]
    public void ToTtlValue_Should_ClampToOneSecond_When_TheDurationHasAlreadyPassed()
    {
        // act and assert
        // A zero or negative TTL would be rejected, and an already-expired message should expire
        // promptly rather than never.
        Assert.Equal("1s", NatsScheduling.ToTtlValue(TimeSpan.FromSeconds(-5)));
    }
}
