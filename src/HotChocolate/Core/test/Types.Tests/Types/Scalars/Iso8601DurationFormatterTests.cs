using System.Text;

namespace HotChocolate.Types;

public sealed class Iso8601DurationFormatterTests
{
    public static TheoryData<TimeSpan, string> ValidDurations => new()
    {
        // Zero
        { TimeSpan.Zero, "PT0S" },

        // Basic date components (no time part when only days)
        { TimeSpan.FromDays(1), "P1D" },
        { TimeSpan.FromDays(2), "P2D" },
        { TimeSpan.FromDays(365), "P365D" },

        // Basic time components
        { TimeSpan.FromHours(1), "PT1H" },
        { TimeSpan.FromMinutes(2), "PT2M" },
        { TimeSpan.FromSeconds(3), "PT3S" },

        // Combined date and time
        { TimeSpan.FromDays(1) + TimeSpan.FromHours(1), "P1DT1H" },
        { TimeSpan.FromDays(1) + TimeSpan.FromMinutes(30), "P1DT30M" },
        { TimeSpan.FromDays(1) + TimeSpan.FromSeconds(45), "P1DT45S" },
        {
            TimeSpan.FromDays(1) + TimeSpan.FromHours(1) + TimeSpan.FromMinutes(1),
            "P1DT1H1M"
        },
        {
            TimeSpan.FromDays(1) + TimeSpan.FromHours(1) + TimeSpan.FromMinutes(1)
            + TimeSpan.FromSeconds(1),
            "P1DT1H1M1S"
        },

        // Multiple time components
        { TimeSpan.FromHours(1) + TimeSpan.FromMinutes(2), "PT1H2M" },
        { TimeSpan.FromHours(1) + TimeSpan.FromSeconds(3), "PT1H3S" },
        { TimeSpan.FromMinutes(2) + TimeSpan.FromSeconds(3), "PT2M3S" },
        { TimeSpan.FromHours(1) + TimeSpan.FromMinutes(2) + TimeSpan.FromSeconds(3), "PT1H2M3S" },

        // Negative durations
        { TimeSpan.FromDays(-1), "-P1D" },
        { TimeSpan.FromHours(-1), "-PT1H" },
        { TimeSpan.FromMinutes(-2), "-PT2M" },
        { TimeSpan.FromSeconds(-3), "-PT3S" },
        { TimeSpan.FromDays(-1) + TimeSpan.FromHours(-1), "-P1DT1H" },
        { TimeSpan.FromHours(-1) + TimeSpan.FromMinutes(-2), "-PT1H2M" },

        // Fractional seconds (with trailing zeros trimmed)
        { TimeSpan.FromTicks(1), "PT0.0000001S" }, // 1 tick = 100ns
        { TimeSpan.FromTicks(10), "PT0.000001S" },
        { TimeSpan.FromTicks(100), "PT0.00001S" },
        { TimeSpan.FromTicks(1_000), "PT0.0001S" },
        { TimeSpan.FromTicks(10_000), "PT0.001S" },
        { TimeSpan.FromTicks(100_000), "PT0.01S" },
        { TimeSpan.FromTicks(1_000_000), "PT0.1S" },
        { TimeSpan.FromMilliseconds(123), "PT0.123S" },
        { TimeSpan.FromMilliseconds(123.456), "PT0.123456S" },
        { TimeSpan.FromSeconds(1.5), "PT1.5S" },
        { TimeSpan.FromSeconds(1.25), "PT1.25S" },
        { TimeSpan.FromSeconds(1.123456), "PT1.123456S" },
        { TimeSpan.FromSeconds(1) + TimeSpan.FromTicks(1), "PT1.0000001S" },

        // Negative fractional seconds
        { TimeSpan.FromTicks(-1), "-PT0.0000001S" },
        { TimeSpan.FromMilliseconds(-1), "-PT0.001S" },
        { TimeSpan.FromSeconds(-1.5), "-PT1.5S" },

        // Large values
        { TimeSpan.FromDays(10_000), "P10000D" },
        { TimeSpan.FromHours(24), "P1D" },
        { TimeSpan.FromHours(25), "P1DT1H" },
        { TimeSpan.FromMinutes(1_440), "P1D" },
        { TimeSpan.FromSeconds(86_400), "P1D" },

        // TimeSpan boundary values
        { TimeSpan.MinValue, "-P10675199DT2H48M5.4775808S" },
        { TimeSpan.MaxValue, "P10675199DT2H48M5.4775807S" },

        // Complex combinations
        {
            TimeSpan.FromDays(10) + TimeSpan.FromHours(5) + TimeSpan.FromMinutes(30)
            + TimeSpan.FromSeconds(15),
            "P10DT5H30M15S"
        },
        {
            TimeSpan.FromDays(100) + TimeSpan.FromHours(23) + TimeSpan.FromMinutes(59)
            + TimeSpan.FromSeconds(59.999),
            "P100DT23H59M59.999S"
        },
        {
            TimeSpan.FromDays(1) + TimeSpan.FromHours(2) + TimeSpan.FromMinutes(3)
            + TimeSpan.FromSeconds(4.5),
            "P1DT2H3M4.5S"
        },

        // Edge cases with only specific components
        { new TimeSpan(0, 0, 0, 0, 1), "PT0.001S" },
        { new TimeSpan(0, 0, 0, 1, 0), "PT1S" },
        { new TimeSpan(0, 0, 1, 0, 0), "PT1M" },
        { new TimeSpan(0, 1, 0, 0, 0), "PT1H" },
        { new TimeSpan(1, 0, 0, 0, 0), "P1D" },

        // Mixed positive time with day
        { new TimeSpan(5, 12, 34, 56, 789), "P5DT12H34M56.789S" }
    };

    [Theory]
    [MemberData(nameof(ValidDurations))]
    public void TryFormat_ValidDuration_ReturnsTrue(TimeSpan duration, string expected)
    {
        // arrange
        Span<byte> buffer = stackalloc byte[64];

        // act
        var result = Iso8601DurationFormatter.TryFormat(duration, buffer, out var bytesWritten);

        // assert
        Assert.True(result);
        var actual = Encoding.UTF8.GetString(buffer[..bytesWritten]);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [MemberData(nameof(ValidDurations))]
    public void Format_ValidDuration_ReturnsCorrectString(TimeSpan duration, string expected)
    {
        // act
        var actual = Iso8601DurationFormatter.Format(duration);

        // assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TryFormat_InsufficientBuffer_ReturnsFalse()
    {
        // arrange
        var duration = TimeSpan.FromDays(1);
        Span<byte> buffer = stackalloc byte[2]; // Too small for "P1D" (needs 3)

        // act
        var result = Iso8601DurationFormatter.TryFormat(duration, buffer, out var bytesWritten);

        // assert
        Assert.False(result);
        Assert.Equal(0, bytesWritten);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void TryFormat_ZeroWithSmallBuffer_ReturnsFalse(int bufferSize)
    {
        // arrange
        var duration = TimeSpan.Zero;
        Span<byte> buffer = stackalloc byte[bufferSize];

        // act
        var result = Iso8601DurationFormatter.TryFormat(duration, buffer, out var bytesWritten);

        // assert
        Assert.False(result);
        Assert.Equal(0, bytesWritten);
    }

    [Fact]
    public void TryFormat_ZeroWithExactBuffer_ReturnsTrue()
    {
        // arrange
        var duration = TimeSpan.Zero;
        Span<byte> buffer = stackalloc byte[4]; // Exact size for "PT0S"

        // act
        var result = Iso8601DurationFormatter.TryFormat(duration, buffer, out var bytesWritten);

        // assert
        Assert.True(result);
        Assert.Equal(4, bytesWritten);
        Assert.Equal("PT0S", Encoding.UTF8.GetString(buffer[..bytesWritten]));
    }

    [Fact]
    public void Format_ProducesValidOutputForParser()
    {
        // arrange
        var durations = new[]
        {
            TimeSpan.Zero,
            TimeSpan.FromDays(1),
            TimeSpan.FromHours(2),
            TimeSpan.FromMinutes(3),
            TimeSpan.FromSeconds(4.5),
            TimeSpan.FromDays(-1) + TimeSpan.FromHours(-2),
            TimeSpan.MinValue,
            TimeSpan.MaxValue
        };

        foreach (var expected in durations)
        {
            // act
            var formatted = Iso8601DurationFormatter.Format(expected);
            var parseSuccess = Iso8601DurationParser.TryParse(formatted.AsSpan(), out var actual);

            // assert
            Assert.True(parseSuccess, $"Failed to parse formatted duration: {formatted}");
            Assert.Equal(expected, actual);
        }
    }
}
