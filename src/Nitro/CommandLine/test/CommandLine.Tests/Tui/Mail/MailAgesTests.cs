using ChilliCream.Nitro.CommandLine.Tui.Mail;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Mail;

public sealed class MailAgesTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(0, "now")]
    [InlineData(30, "now")]
    [InlineData(90, "1m")]
    [InlineData(3600, "1h")]
    [InlineData(7200, "2h")]
    [InlineData(90000, "1d")]
    public void Format_Should_ReturnRelativeLabel_When_MessageIsUnderAWeekOld(int elapsedSeconds, string expected)
    {
        // arrange
        var createdAt = Now.AddSeconds(-elapsedSeconds);

        // act
        var formatted = MailAges.Format(createdAt, Now);

        // assert
        Assert.Equal(expected, formatted);
    }

    [Fact]
    public void Format_Should_ReturnIsoDate_When_MessageIsAtLeastAWeekOld()
    {
        // arrange
        var createdAt = Now.AddDays(-8);

        // act
        var formatted = MailAges.Format(createdAt, Now);

        // assert
        Assert.Equal(createdAt.ToString("yyyy-MM-dd"), formatted);
    }

    [Fact]
    public void Format_Should_ReturnNow_When_CreatedAtIsInTheFuture()
    {
        // arrange
        var createdAt = Now.AddMinutes(5);

        // act
        var formatted = MailAges.Format(createdAt, Now);

        // assert
        Assert.Equal("now", formatted);
    }
}
