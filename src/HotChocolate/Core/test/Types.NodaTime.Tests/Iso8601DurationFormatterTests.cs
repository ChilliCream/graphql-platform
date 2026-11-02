using System.Text;
using NodaTime;

namespace HotChocolate.Types.NodaTime;

public sealed class Iso8601DurationFormatterTests
{
    public static TheoryData<Duration, string> ValidDurations => new()
    {
        // Zero
        { Duration.Zero, "PT0S" },

        // Basic date components (no time part when only days)
        { Duration.FromDays(1), "P1D" },
        { Duration.FromDays(2), "P2D" },
        { Duration.FromDays(365), "P365D" },

        // Basic time components
        { Duration.FromHours(1), "PT1H" },
        { Duration.FromMinutes(2), "PT2M" },
        { Duration.FromSeconds(3), "PT3S" },

        // Combined date and time
        { Duration.FromDays(1) + Duration.FromHours(1), "P1DT1H" },
        { Duration.FromDays(1) + Duration.FromMinutes(30), "P1DT30M" },
        { Duration.FromDays(1) + Duration.FromSeconds(45), "P1DT45S" },
        {
            Duration.FromDays(1) + Duration.FromHours(1) + Duration.FromMinutes(1),
            "P1DT1H1M"
        },
        {
            Duration.FromDays(1) + Duration.FromHours(1) + Duration.FromMinutes(1)
            + Duration.FromSeconds(1),
            "P1DT1H1M1S"
        },

        // Multiple time components
        { Duration.FromHours(1) + Duration.FromMinutes(2), "PT1H2M" },
        { Duration.FromHours(1) + Duration.FromSeconds(3), "PT1H3S" },
        { Duration.FromMinutes(2) + Duration.FromSeconds(3), "PT2M3S" },
        { Duration.FromHours(1) + Duration.FromMinutes(2) + Duration.FromSeconds(3), "PT1H2M3S" },

        // Negative durations
        { Duration.FromDays(-1), "-P1D" },
        { Duration.FromHours(-1), "-PT1H" },
        { Duration.FromMinutes(-2), "-PT2M" },
        { Duration.FromSeconds(-3), "-PT3S" },
        { Duration.FromDays(-1) + Duration.FromHours(-1), "-P1DT1H" },
        { Duration.FromHours(-1) + Duration.FromMinutes(-2), "-PT1H2M" },

        // Fractional seconds (with trailing zeros trimmed)
        { Duration.FromNanoseconds(1), "PT0.000000001S" },
        { Duration.FromNanoseconds(10), "PT0.00000001S" },
        { Duration.FromNanoseconds(100), "PT0.0000001S" },
        { Duration.FromNanoseconds(1_000), "PT0.000001S" },
        { Duration.FromNanoseconds(10_000), "PT0.00001S" },
        { Duration.FromNanoseconds(100_000), "PT0.0001S" },
        { Duration.FromNanoseconds(1_000_000), "PT0.001S" },
        { Duration.FromNanoseconds(10_000_000), "PT0.01S" },
        { Duration.FromNanoseconds(100_000_000), "PT0.1S" },
        { Duration.FromMilliseconds(123), "PT0.123S" },
        { Duration.FromNanoseconds(123_456_000), "PT0.123456S" },
        { Duration.FromSeconds(1.5), "PT1.5S" },
        { Duration.FromSeconds(1.25), "PT1.25S" },
        { Duration.FromNanoseconds(1_123_456_000), "PT1.123456S" },
        { Duration.FromSeconds(1) + Duration.FromNanoseconds(1), "PT1.000000001S" },

        // Negative fractional seconds
        { Duration.FromNanoseconds(-1), "-PT0.000000001S" },
        { Duration.FromMilliseconds(-1), "-PT0.001S" },
        { Duration.FromSeconds(-1.5), "-PT1.5S" },

        // Large values
        { Duration.FromDays(10000), "P10000D" },
        { Duration.FromHours(24), "P1D" },
        { Duration.FromHours(25), "P1DT1H" },
        { Duration.FromMinutes(1440), "P1D" },
        { Duration.FromSeconds(86400), "P1D" },

        // Duration boundary values
        { Duration.MinValue, "-P16777216D" },
        { Duration.MaxValue, "P16777215DT23H59M59.999999999S" },

        // Complex combinations
        {
            Duration.FromDays(10) + Duration.FromHours(5) + Duration.FromMinutes(30)
            + Duration.FromSeconds(15),
            "P10DT5H30M15S"
        },
        {
            Duration.FromDays(100) + Duration.FromHours(23) + Duration.FromMinutes(59)
            + Duration.FromSeconds(59) + Duration.FromMilliseconds(999),
            "P100DT23H59M59.999S"
        },
        {
            Duration.FromDays(1) + Duration.FromHours(2) + Duration.FromMinutes(3)
            + Duration.FromSeconds(4.5),
            "P1DT2H3M4.5S"
        },

        // Edge cases with only specific components
        { Duration.FromMilliseconds(1), "PT0.001S" },
        { Duration.FromSeconds(1), "PT1S" },
        { Duration.FromMinutes(1), "PT1M" },
        { Duration.FromHours(1), "PT1H" },
        { Duration.FromDays(1), "P1D" },

        // Mixed positive time with day
        {
            Duration.FromDays(5) + Duration.FromHours(12) + Duration.FromMinutes(34)
            + Duration.FromSeconds(56) + Duration.FromMilliseconds(789),
            "P5DT12H34M56.789S"
        }
    };

    [Theory]
    [MemberData(nameof(ValidDurations))]
    public void TryFormat_ValidDuration_ReturnsTrue(Duration duration, string expected)
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
    public void Format_ValidDuration_ReturnsCorrectString(Duration duration, string expected)
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
        var duration = Duration.FromDays(1);
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
        var duration = Duration.Zero;
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
        var duration = Duration.Zero;
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
            Duration.Zero,
            Duration.FromDays(1),
            Duration.FromHours(2),
            Duration.FromMinutes(3),
            Duration.FromSeconds(4.5),
            Duration.FromDays(-1) + Duration.FromHours(-2),
            Duration.MinValue,
            Duration.MaxValue
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
