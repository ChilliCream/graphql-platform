using System.Text;
using NodaTime;

namespace HotChocolate.Types.NodaTime;

public sealed class Iso8601DurationParserTests
{
    public static TheoryData<string, Duration> ValidDurations => new()
    {
        // Basic date components
        { "P1Y", Duration.FromDays(365) },
        { "P2M", Duration.FromDays(60) },
        { "P3W", Duration.FromDays(21) },
        { "P4D", Duration.FromDays(4) },

        // Basic time components
        { "PT1H", Duration.FromHours(1) },
        { "PT2M", Duration.FromMinutes(2) },
        { "PT3S", Duration.FromSeconds(3) },

        // Combined date and time
        { "P1DT1H", Duration.FromDays(1) + Duration.FromHours(1) },
        { "P1DT1H1M", Duration.FromDays(1) + Duration.FromHours(1) + Duration.FromMinutes(1) },
        {
            "P1DT1H1M1S",
            Duration.FromDays(1) + Duration.FromHours(1) + Duration.FromMinutes(1) + Duration.FromSeconds(1)
        },

        // Multiple date components
        { "P1Y2M3D", Duration.FromDays(365 + 60 + 3) },
        { "P1Y2M3W4D", Duration.FromDays(365 + 60 + 21 + 4) },

        // Multiple time components
        { "PT1H2M3S", Duration.FromHours(1) + Duration.FromMinutes(2) + Duration.FromSeconds(3) },

        // Overall negative duration
        { "-P1D", Duration.FromDays(-1) },
        { "-PT1H", Duration.FromHours(-1) },
        { "-P1DT1H", Duration.FromDays(-1) + Duration.FromHours(-1) },

        // Per-component negative (ISO 8601-2:2019)
        { "P-1D", Duration.FromDays(-1) },
        { "PT-1H", Duration.FromHours(-1) },
        { "P1DT-1H", Duration.FromDays(1) + Duration.FromHours(-1) },
        { "P-1DT1H", Duration.FromDays(-1) + Duration.FromHours(1) },

        // Overall negative with per-component negative (double negative = positive)
        { "-P-1D", Duration.FromDays(1) },
        { "-PT-1H", Duration.FromHours(1) },

        // Fractional seconds with dot
        { "PT0.1S", Duration.FromNanoseconds(100_000_000) },
        { "PT0.01S", Duration.FromNanoseconds(10_000_000) },
        { "PT0.001S", Duration.FromMilliseconds(1) },
        { "PT0.0001S", Duration.FromNanoseconds(100_000) },
        { "PT0.00001S", Duration.FromNanoseconds(10_000) },
        { "PT0.000001S", Duration.FromNanoseconds(1_000) },
        { "PT0.0000001S", Duration.FromNanoseconds(100) },
        { "PT0.00000001S", Duration.FromNanoseconds(10) },
        { "PT0.000000001S", Duration.FromNanoseconds(1) },
        { "PT1.5S", Duration.FromSeconds(1.5) },

        // Fractional seconds with comma (ISO 8601 alternative)
        { "PT0,5S", Duration.FromSeconds(0.5) },
        { "PT1,25S", Duration.FromSeconds(1.25) },

        // Negative fractional seconds
        { "-PT0.000000001S", Duration.FromNanoseconds(-1) },
        { "PT-0.5S", Duration.FromSeconds(-0.5) },

        // Large values
        { "P365D", Duration.FromDays(365) },
        { "PT24H", Duration.FromHours(24) },
        { "PT1440M", Duration.FromMinutes(1440) },
        { "PT86400S", Duration.FromSeconds(86400) },

        // Zero values
        { "P0D", Duration.Zero },
        { "PT0S", Duration.Zero },
        { "P0DT0H0M0S", Duration.Zero },

        // Edge case: excess fractional digits (should be truncated)
        { "PT0.0000000001S", Duration.Zero }, // Beyond nanosecond precision
        { "PT0.0000000009S", Duration.Zero }, // Rounded down
        { "PT1.123456789S", Duration.FromNanoseconds(1_123_456_789) },
        { "PT1.1234567899S", Duration.FromNanoseconds(1_123_456_789) }, // Truncated

        // Duration boundary values
        { "-P16777216D", Duration.MinValue },
        { "P16777215DT23H59M59.999999999S", Duration.MaxValue },

        // Complex combinations
        {
            "P1Y2M3W4DT5H6M7S",
            Duration.FromDays(365 + 60 + 21 + 4) + Duration.FromHours(5) + Duration.FromMinutes(6)
            + Duration.FromSeconds(7)
        },
        {
            "P1Y2M3W4DT5H6M7.8S",
            Duration.FromDays(365 + 60 + 21 + 4) + Duration.FromHours(5) + Duration.FromMinutes(6)
            + Duration.FromSeconds(7.8)
        }
    };

    public static TheoryData<string> InvalidDurations =>
    [
        // Empty or whitespace
        "",
        " ",

        // Missing P designator
        "1D",
        "T1H",

        // Only P designator
        "P",

        // Only PT designator
        "PT",

        // Invalid component order (time before T)
        "P1H",

        // Missing designator
        "P1",
        "PT1",

        // Invalid designators
        "P1X",
        "PT1X",

        // Fractional on non-seconds
        "P1.5D",
        "PT1.5H",
        "PT1.5M",

        // Multiple T designators
        "PT1HT1M",

        // T without time components
        "P1DT",

        // Invalid characters
        "P1D!",
        "PT@1H",

        // Component without value
        "PD",
        "PTH",

        // Double negative
        "P--1D",

        // Negative after designator
        "P1-D",

        // Fractional without an integer part (debatable, but parser requires it)
        "PT.5S",

        // Decimal separator without fractional digits
        "PT1.S",
        "PT1,S",
        "PT0.S",
        "PT0,S",

        // Out-of-order components
        "PT1S1H",
        "PT1M1H",
        "P1D1Y",
        "P1D1M",
        "P1W1Y",
        "P1D1W",

        // Duplicated components
        "P1Y1Y",
        "P1M1M",
        "P1W1W",
        "P1D1D",
        "PT1H1H",
        "PT1M1M",
        "PT1S1S",

        // Overflow - exceeds Duration.MaxValue
        "P16777217D",
        "-P16777217D",
        "PT402653184H",
        "P99999999Y"
    ];

    [Theory]
    [MemberData(nameof(ValidDurations))]
    public void TryParse_ValidDuration_ReturnsTrue(string duration, Duration expected)
    {
        // act
        var result = Iso8601DurationParser.TryParse(duration.AsSpan(), out var actual);

        // assert
        Assert.True(result);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [MemberData(nameof(InvalidDurations))]
    public void TryParse_InvalidDuration_ReturnsFalse(string duration)
    {
        // act
        var result = Iso8601DurationParser.TryParse(duration.AsSpan(), out var actual);

        // assert
        Assert.False(result);
        Assert.Equal(Duration.Zero, actual);
    }

    [Theory]
    [MemberData(nameof(ValidDurations))]
    public void TryParse_Utf8_ValidDuration_ReturnsTrue(string duration, Duration expected)
    {
        // arrange
        var utf8Bytes = Encoding.UTF8.GetBytes(duration);

        // act
        var result = Iso8601DurationParser.TryParse(utf8Bytes.AsSpan(), out var actual);

        // assert
        Assert.True(result);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [MemberData(nameof(InvalidDurations))]
    public void TryParse_Utf8_InvalidDuration_ReturnsFalse(string duration)
    {
        // arrange
        var utf8Bytes = Encoding.UTF8.GetBytes(duration);

        // act
        var result = Iso8601DurationParser.TryParse(utf8Bytes.AsSpan(), out var actual);

        // assert
        Assert.False(result);
        Assert.Equal(Duration.Zero, actual);
    }
}
