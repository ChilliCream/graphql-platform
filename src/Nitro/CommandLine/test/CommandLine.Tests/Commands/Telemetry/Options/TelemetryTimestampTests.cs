using System.CommandLine;
using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Telemetry.Options;

public sealed class TelemetryTimestampTests
{
    [Theory]
    [InlineData("90s", "2026-01-01T11:58:30Z")]
    [InlineData("30m", "2026-01-01T11:30:00Z")]
    [InlineData("2h", "2026-01-01T10:00:00Z")]
    [InlineData("7d", "2025-12-25T12:00:00Z")]
    public void TryParse_Should_ParseDuration_When_ValueUsesSupportedUnit(string value, string expectedTimestamp)
    {
        // arrange
        var now = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

        // act
        var success = TelemetryTimestamp.TryParse(
            value,
            now,
            "--since",
            enforceMaximumAge: true,
            out var timestamp,
            out var error);

        // assert
        Assert.True(success);
        Assert.Equal(DateTimeOffset.Parse(expectedTimestamp), timestamp);
        Assert.Null(error);
    }

    [Fact]
    public void TryParse_Should_ParseTimestamp_When_ValueIsIso8601()
    {
        // arrange
        var now = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

        // act
        var success = TelemetryTimestamp.TryParse(
            "2025-12-31T23:30:00Z",
            now,
            "--since",
            enforceMaximumAge: true,
            out var timestamp,
            out var error);

        // assert
        Assert.True(success);
        Assert.Equal(new DateTimeOffset(2025, 12, 31, 23, 30, 0, TimeSpan.Zero), timestamp);
        Assert.Null(error);
    }

    [Fact]
    public void TryParse_Should_ReturnError_When_ValueIsInvalid()
    {
        // arrange
        var now = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

        // act
        var success = TelemetryTimestamp.TryParse(
            "yesterday",
            now,
            "--since",
            enforceMaximumAge: true,
            out _,
            out var error);

        // assert
        Assert.False(success);
        Assert.Equal(
            """
            Option '--since' received an invalid value: yesterday
            """,
            error);
    }

    [Theory]
    [InlineData("61d")]
    [InlineData("2025-10-31T12:00:00Z")]
    public void TryParse_Should_ReturnError_When_SinceIsOlderThanSixtyDays(string value)
    {
        // arrange
        var now = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

        // act
        var success = TelemetryTimestamp.TryParse(value, now, "--since", enforceMaximumAge: true, out _, out var error);

        // assert
        Assert.False(success);
        Assert.Equal(
            """
            Option '--since' cannot be more than 60 days in the past.
            """,
            error);
    }

    [Fact]
    public void TryParse_Should_ReturnError_When_DurationOversizedUnderflows()
    {
        // arrange
        var now = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

        // act
        var success = TelemetryTimestamp.TryParse(
            "999999d",
            now,
            "--until",
            enforceMaximumAge: false,
            out _,
            out var error);

        // assert
        Assert.False(success);
        Assert.Equal(
            """
            Option '--until' cannot be more than 60 days in the past.
            """,
            error);
    }

    [Theory]
    [InlineData("--since")]
    [InlineData("--until")]
    public void Parse_Should_ReportError_When_TimestampValueIsInvalid(string optionName)
    {
        // arrange
        var now = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        using var provider = new ServiceCollection()
            .AddSingleton<TimeProvider>(new FakeTimeProvider(now))
            .BuildServiceProvider();
        CommandExecutionContext.Initialize(new CommandServices(provider));
        var command = new Command("telemetry");
        command.Options.Add(
            optionName == TelemetrySinceOption.OptionName ? new TelemetrySinceOption() : new TelemetryUntilOption());

        // act
        var result = command.Parse([optionName, "yesterday"]);

        // assert
        Assert.Equal(
            $"""
            Option '{optionName}' received an invalid value: yesterday
            """,
            Assert.Single(result.Errors).Message);
    }
}
