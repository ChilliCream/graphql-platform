using System.Text;

namespace HotChocolate.Types;

public sealed class Iso8601DurationParserTests
{
    public static TheoryData<string, TimeSpan> ValidDurations => new()
    {
        // Basic date components
        { "P1Y", TimeSpan.FromDays(365) },
        { "P2M", TimeSpan.FromDays(60) },
        { "P3W", TimeSpan.FromDays(21) },
        { "P4D", TimeSpan.FromDays(4) },

        // Basic time components
        { "PT1H", TimeSpan.FromHours(1) },
        { "PT2M", TimeSpan.FromMinutes(2) },
        { "PT3S", TimeSpan.FromSeconds(3) },

        // Combined date and time
        { "P1DT1H", TimeSpan.FromDays(1) + TimeSpan.FromHours(1) },
        { "P1DT1H1M", TimeSpan.FromDays(1) + TimeSpan.FromHours(1) + TimeSpan.FromMinutes(1) },
        {
            "P1DT1H1M1S",
            TimeSpan.FromDays(1) + TimeSpan.FromHours(1) + TimeSpan.FromMinutes(1) + TimeSpan.FromSeconds(1)
        },

        // Multiple date components
        { "P1Y2M3D", TimeSpan.FromDays(365 + 60 + 3) },
        { "P1Y2M3W4D", TimeSpan.FromDays(365 + 60 + 21 + 4) },

        // Multiple time components
        { "PT1H2M3S", TimeSpan.FromHours(1) + TimeSpan.FromMinutes(2) + TimeSpan.FromSeconds(3) },

        // Overall negative duration
        { "-P1D", TimeSpan.FromDays(-1) },
        { "-PT1H", TimeSpan.FromHours(-1) },
        { "-P1DT1H", TimeSpan.FromDays(-1) + TimeSpan.FromHours(-1) },

        // Per-component negative (ISO 8601-2:2019)
        { "P-1D", TimeSpan.FromDays(-1) },
        { "PT-1H", TimeSpan.FromHours(-1) },
        { "P1DT-1H", TimeSpan.FromDays(1) + TimeSpan.FromHours(-1) },
        { "P-1DT1H", TimeSpan.FromDays(-1) + TimeSpan.FromHours(1) },

        // Overall negative with per-component negative (double negative = positive)
        { "-P-1D", TimeSpan.FromDays(1) },
        { "-PT-1H", TimeSpan.FromHours(1) },

        // Fractional seconds with dot
        { "PT0.1S", TimeSpan.FromTicks(TimeSpan.TicksPerSecond / 10) },
        { "PT0.01S", TimeSpan.FromTicks(TimeSpan.TicksPerSecond / 100) },
        { "PT0.001S", TimeSpan.FromMilliseconds(1) },
        { "PT0.0001S", TimeSpan.FromTicks(TimeSpan.TicksPerSecond / 10000) },
        { "PT0.00001S", TimeSpan.FromTicks(TimeSpan.TicksPerSecond / 100000) },
        { "PT0.000001S", TimeSpan.FromTicks(TimeSpan.TicksPerSecond / 1000000) },
        { "PT0.0000001S", TimeSpan.FromTicks(1) }, // 100ns = 1 tick
        { "PT1.5S", TimeSpan.FromSeconds(1.5) },

        // Fractional seconds with comma (ISO 8601 alternative)
        { "PT0,5S", TimeSpan.FromSeconds(0.5) },
        { "PT1,25S", TimeSpan.FromSeconds(1.25) },

        // Negative fractional seconds
        { "-PT0.0000001S", TimeSpan.FromMilliseconds(-1) / 1000 / 10 },
        { "PT-0.5S", TimeSpan.FromSeconds(-0.5) },

        // Large values
        { "P365D", TimeSpan.FromDays(365) },
        { "PT24H", TimeSpan.FromHours(24) },
        { "PT1440M", TimeSpan.FromMinutes(1440) },
        { "PT86400S", TimeSpan.FromSeconds(86400) },

        // Zero values
        { "P0D", TimeSpan.Zero },
        { "PT0S", TimeSpan.Zero },
        { "P0DT0H0M0S", TimeSpan.Zero },

        // Edge case: excess fractional digits (should be truncated)
        { "PT0.00000001S", TimeSpan.Zero }, // Beyond tick precision
        { "PT0.00000009S", TimeSpan.Zero }, // Rounded down
        { "PT1.123456789S", TimeSpan.FromTicks(11234567) }, // Only first 7 digits

        // TimeSpan boundary values
        { "-P10675199DT2H48M5.4775808S", TimeSpan.MinValue },
        { "P10675199DT2H48M5.4775807S", TimeSpan.MaxValue },

        // Complex combinations
        {
            "P1Y2M3W4DT5H6M7S",
            TimeSpan.FromDays(365 + 60 + 21 + 4) + TimeSpan.FromHours(5) + TimeSpan.FromMinutes(6)
            + TimeSpan.FromSeconds(7)
        },
        {
            "P1Y2M3W4DT5H6M7.8S",
            TimeSpan.FromDays(365 + 60 + 21 + 4) + TimeSpan.FromHours(5) + TimeSpan.FromMinutes(6)
            + TimeSpan.FromSeconds(7.8)
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

        // Overflow - exceeds TimeSpan.MaxValue
        "P10675200D",
        "-P10675200D",
        "PT2562047788015216H",
        "P99999999Y"
    ];

    [Theory]
    [MemberData(nameof(ValidDurations))]
    public void TryParse_ValidDuration_ReturnsTrue(string duration, TimeSpan expected)
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
        Assert.Equal(TimeSpan.Zero, actual);
    }

    [Theory]
    [MemberData(nameof(ValidDurations))]
    public void TryParse_Utf8_ValidDuration_ReturnsTrue(string duration, TimeSpan expected)
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
        Assert.Equal(TimeSpan.Zero, actual);
    }
}
