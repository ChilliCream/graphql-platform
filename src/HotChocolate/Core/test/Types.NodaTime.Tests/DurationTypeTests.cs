using System.Text;
using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Text.Json;
using NodaTime;

namespace HotChocolate.Types.NodaTime.Tests;

public class DurationTypeTests
{
    [Fact]
    public void Ensure_Type_Name_Is_Correct()
    {
        var type = new DurationType();
        Assert.Equal("Duration", type.Name);
    }

    // ----------------------------------------------------------------
    // CoerceInputLiteral — valid
    // ----------------------------------------------------------------

    [Theory]
    [MemberData(nameof(ValidDurations))]
    public void CoerceInputLiteral_Valid(string iso, Duration expected)
    {
        var type = new DurationType();
        var literal = new StringValueNode(iso);

        var result = (Duration)type.CoerceInputLiteral(literal);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void CoerceInputLiteral_Zero()
    {
        var type = new DurationType();
        var literal = new StringValueNode("PT0S");

        var result = (Duration)type.CoerceInputLiteral(literal);

        Assert.Equal(Duration.Zero, result);
    }

    // ----------------------------------------------------------------
    // CoerceInputLiteral — invalid
    // ----------------------------------------------------------------

    [Theory]
    [MemberData(nameof(InvalidDurations))]
    public void CoerceInputLiteral_Invalid(string iso)
    {
        var type = new DurationType();
        var literal = new StringValueNode(iso);

        Assert.Throws<LeafCoercionException>(() => type.CoerceInputLiteral(literal));
    }

    [Fact]
    public void CoerceInputLiteral_Wrong_Literal_Type_Throws()
    {
        var type = new DurationType();
        Assert.Throws<LeafCoercionException>(() => type.CoerceInputLiteral(new IntValueNode(42)));
    }

    // ----------------------------------------------------------------
    // CoerceInputValue — valid
    // ----------------------------------------------------------------

    [Theory]
    [MemberData(nameof(ValidDurations))]
    public void CoerceInputValue_Valid(string iso, Duration expected)
    {
        var type = new DurationType();
        var inputValue = ParseInputValue($"\"{iso}\"");

        var result = (Duration)type.CoerceInputValue(inputValue, null!);

        Assert.Equal(expected, result);
    }

    // ----------------------------------------------------------------
    // CoerceInputValue — invalid
    // ----------------------------------------------------------------

    [Theory]
    [MemberData(nameof(InvalidDurations))]
    public void CoerceInputValue_Invalid(string iso)
    {
        var type = new DurationType();
        var inputValue = ParseInputValue($"\"{iso}\"");

        Assert.Throws<LeafCoercionException>(() => type.CoerceInputValue(inputValue, null!));
    }

    // ----------------------------------------------------------------
    // CoerceOutputValue — valid
    // ----------------------------------------------------------------

    [Theory]
    [MemberData(nameof(OutputCases))]
    public void CoerceOutputValue_Valid(Duration duration, string expected)
    {
        var type = new DurationType();
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");

        type.CoerceOutputValue(duration, resultValue);

        Assert.Equal(expected, resultValue.GetString());
    }

    [Fact]
    public void CoerceOutputValue_Zero()
    {
        var type = new DurationType();
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");

        type.CoerceOutputValue(Duration.Zero, resultValue);

        Assert.Equal("PT0S", resultValue.GetString());
    }

    // ----------------------------------------------------------------
    // CoerceOutputValue — invalid
    // ----------------------------------------------------------------

    [Fact]
    public void CoerceOutputValue_Invalid_Type_Throws()
    {
        var type = new DurationType();
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");

        Assert.Throws<LeafCoercionException>(() => type.CoerceOutputValue("P1D", resultValue));
    }

    // ----------------------------------------------------------------
    // ValueToLiteral — valid
    // ----------------------------------------------------------------

    [Theory]
    [MemberData(nameof(OutputCases))]
    public void ValueToLiteral_Valid(Duration duration, string expected)
    {
        var type = new DurationType();

        var literal = type.ValueToLiteral(duration);

        Assert.Equal(expected, Assert.IsType<StringValueNode>(literal).Value);
    }

    [Fact]
    public void ValueToLiteral_Zero()
    {
        var type = new DurationType();

        var literal = type.ValueToLiteral(Duration.Zero);

        Assert.Equal("PT0S", Assert.IsType<StringValueNode>(literal).Value);
    }

    // ----------------------------------------------------------------
    // ValueToLiteral — invalid
    // ----------------------------------------------------------------

    [Fact]
    public void ValueToLiteral_Invalid_Type_Throws()
    {
        var type = new DurationType();
        Assert.Throws<LeafCoercionException>(() => type.ValueToLiteral("PT1H"));
    }

    // ----------------------------------------------------------------
    // Round-trip: parse canonical ISO → format → same string
    // ----------------------------------------------------------------

    [Theory]
    [InlineData("PT0S")]
    [InlineData("P1D")]
    [InlineData("P7D")]
    [InlineData("PT1H")]
    [InlineData("PT30M")]
    [InlineData("PT1S")]
    [InlineData("PT0.5S")]
    [InlineData("PT1.123456789S")]
    [InlineData("P1DT2H30M")]
    [InlineData("P365D")]
    [InlineData("-P1D")]
    [InlineData("-PT30M")]
    [InlineData("-PT0.000000001S")]
    [InlineData("P1DT1H1M1.5S")]
    public void RoundTrip_Canonical(string iso)
    {
        var type = new DurationType();

        // Parse.
        var literal = new StringValueNode(iso);
        var duration = (Duration)type.CoerceInputLiteral(literal);

        // Format back to string.
        var output = Assert.IsType<StringValueNode>(type.ValueToLiteral(duration));

        Assert.Equal(iso, output.Value);
    }

    // ----------------------------------------------------------------
    // Parser: combined date components
    // ----------------------------------------------------------------

    [Fact]
    public void CoerceInputLiteral_Weeks_Combined()
    {
        // Mirrors core DurationTypeTests.CoerceInputLiteral_Weeks: "P2M2W5D" → 79 days
        var type = new DurationType();
        var literal = new StringValueNode("P2M2W5D");

        var result = (Duration)type.CoerceInputLiteral(literal);

        // 2M=60d, 2W=14d, 5D=5d → 79 days
        Assert.Equal(Duration.FromDays(79), result);
    }

    // ----------------------------------------------------------------
    // Parser: input cannot end with digits (no designator)
    // ----------------------------------------------------------------

    [Fact]
    public void CoerceInputLiteral_CannotEndWithDigits()
    {
        var type = new DurationType();
        var literal = new StringValueNode("PT5");

        Assert.Throws<LeafCoercionException>(() => type.CoerceInputLiteral(literal));
    }

    // ----------------------------------------------------------------
    // Large value round-trips
    // ----------------------------------------------------------------

    [Fact]
    public void CoerceInputLiteral_LargePositive()
    {
        var type = new DurationType();
        var literal = new StringValueNode("P10675199DT2H48M5.4775807S");

        var result = (Duration)type.CoerceInputLiteral(literal);

        var expected = Duration.FromDays(10675199)
            + Duration.FromHours(2)
            + Duration.FromMinutes(48)
            + Duration.FromSeconds(5)
            + Duration.FromNanoseconds(477_580_700);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void CoerceInputLiteral_LargeNegative()
    {
        var type = new DurationType();
        var literal = new StringValueNode("-P10675199DT2H48M5.4775808S");

        var result = (Duration)type.CoerceInputLiteral(literal);

        var expected = -(Duration.FromDays(10675199)
            + Duration.FromHours(2)
            + Duration.FromMinutes(48)
            + Duration.FromSeconds(5)
            + Duration.FromNanoseconds(477_580_800));
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ValueToLiteral_LargePositive()
    {
        var type = new DurationType();
        var duration = Duration.FromDays(10675199)
            + Duration.FromHours(2)
            + Duration.FromMinutes(48)
            + Duration.FromSeconds(5)
            + Duration.FromNanoseconds(477_580_700);

        var literal = type.ValueToLiteral(duration);

        Assert.Equal(
            "P10675199DT2H48M5.4775807S",
            Assert.IsType<StringValueNode>(literal).Value);
    }

    [Fact]
    public void ValueToLiteral_LargeNegative()
    {
        var type = new DurationType();
        var duration = -(Duration.FromDays(10675199)
            + Duration.FromHours(2)
            + Duration.FromMinutes(48)
            + Duration.FromSeconds(5)
            + Duration.FromNanoseconds(477_580_800));

        var literal = type.ValueToLiteral(duration);

        Assert.Equal(
            "-P10675199DT2H48M5.4775808S",
            Assert.IsType<StringValueNode>(literal).Value);
    }

    // ----------------------------------------------------------------
    // Parser: per-component negative signs (ISO 8601-2:2019)
    // ----------------------------------------------------------------

    [Fact]
    public void Parser_PerComponentNegative()
    {
        var type = new DurationType();

        // "P-1D" means -1 day.
        var literal = new StringValueNode("P-1D");
        var result = (Duration)type.CoerceInputLiteral(literal);

        Assert.Equal(-Duration.FromDays(1), result);
    }

    [Fact]
    public void Parser_OverallNegative_WithComponentNegative()
    {
        var type = new DurationType();

        // "-P-1D" means negate(-1 day) = +1 day.
        var literal = new StringValueNode("-P-1D");
        var result = (Duration)type.CoerceInputLiteral(literal);

        Assert.Equal(Duration.FromDays(1), result);
    }

    [Fact]
    public void Parser_MixedComponentSigns()
    {
        var type = new DurationType();

        // "P1DT-2H" means 1 day minus 2 hours = 22 hours.
        var literal = new StringValueNode("P1DT-2H");
        var result = (Duration)type.CoerceInputLiteral(literal);

        Assert.Equal(Duration.FromHours(22), result);
    }

    // ----------------------------------------------------------------
    // Parser: date components (Y, M, W)
    // ----------------------------------------------------------------

    [Fact]
    public void Parser_Years()
    {
        var type = new DurationType();
        var literal = new StringValueNode("P1Y");
        var result = (Duration)type.CoerceInputLiteral(literal);

        Assert.Equal(Duration.FromDays(365), result);
    }

    [Fact]
    public void Parser_Months()
    {
        var type = new DurationType();
        var literal = new StringValueNode("P1M");
        var result = (Duration)type.CoerceInputLiteral(literal);

        Assert.Equal(Duration.FromDays(30), result);
    }

    [Fact]
    public void Parser_Weeks()
    {
        var type = new DurationType();
        var literal = new StringValueNode("P2W");
        var result = (Duration)type.CoerceInputLiteral(literal);

        Assert.Equal(Duration.FromDays(14), result);
    }

    [Fact]
    public void Parser_AllComponents()
    {
        var type = new DurationType();
        var literal = new StringValueNode("P1Y2M3W4DT5H6M7.5S");
        var result = (Duration)type.CoerceInputLiteral(literal);

        // 1Y=365d, 2M=60d, 3W=21d, 4D=4d → 450 days total
        var expected = Duration.FromDays(450)
            + Duration.FromHours(5)
            + Duration.FromMinutes(6)
            + Duration.FromSeconds(7)
            + Duration.FromNanoseconds(500_000_000);

        Assert.Equal(expected, result);
    }

    // ----------------------------------------------------------------
    // Parser: fractional seconds
    // ----------------------------------------------------------------

    [Fact]
    public void Parser_FractionalSeconds_1Digit()
    {
        var type = new DurationType();
        var literal = new StringValueNode("PT0.5S");
        var result = (Duration)type.CoerceInputLiteral(literal);

        Assert.Equal(Duration.FromNanoseconds(500_000_000), result);
    }

    [Fact]
    public void Parser_FractionalSeconds_9Digits()
    {
        var type = new DurationType();
        var literal = new StringValueNode("PT1.123456789S");
        var result = (Duration)type.CoerceInputLiteral(literal);

        Assert.Equal(
            Duration.FromSeconds(1) + Duration.FromNanoseconds(123_456_789),
            result);
    }

    [Fact]
    public void Parser_FractionalSeconds_Comma()
    {
        var type = new DurationType();
        var literal = new StringValueNode("PT1,5S");
        var result = (Duration)type.CoerceInputLiteral(literal);

        Assert.Equal(
            Duration.FromSeconds(1) + Duration.FromNanoseconds(500_000_000),
            result);
    }

    [Fact]
    public void Parser_FractionalSeconds_ExcessDigitsTruncated()
    {
        var type = new DurationType();

        // More than 9 fractional digits: the 10th digit is discarded.
        var literal = new StringValueNode("PT1.1234567891S");
        var result = (Duration)type.CoerceInputLiteral(literal);

        Assert.Equal(
            Duration.FromSeconds(1) + Duration.FromNanoseconds(123_456_789),
            result);
    }

    // ----------------------------------------------------------------
    // UTF-8 input: StringValueNode stores UTF-8 bytes internally,
    // so CoerceInputLiteral exercises the UTF-8 parser path.
    // ----------------------------------------------------------------

    [Fact]
    public void CoerceInputLiteral_Utf8Path()
    {
        var type = new DurationType();
        var literal = new StringValueNode("P1DT2H");

        var result = (Duration)type.CoerceInputLiteral(literal);

        Assert.Equal(Duration.FromHours(26), result);
    }

    // ----------------------------------------------------------------
    // Test data
    // ----------------------------------------------------------------

    public static TheoryData<string, Duration> ValidDurations()
    {
        return new TheoryData<string, Duration>
        {
            // Zero.
            { "PT0S", Duration.Zero },
            { "P0D", Duration.Zero },

            // Days.
            { "P1D", Duration.FromDays(1) },
            { "P7D", Duration.FromDays(7) },

            // Time components.
            { "PT1H", Duration.FromHours(1) },
            { "PT30M", Duration.FromMinutes(30) },
            { "PT1S", Duration.FromSeconds(1) },

            // Composite.
            { "P1DT2H30M", Duration.FromDays(1) + Duration.FromHours(2) + Duration.FromMinutes(30) },

            // Weeks.
            { "P2W", Duration.FromDays(14) },

            // Years and months (approximate: 1Y=365d, 1M=30d).
            { "P1Y", Duration.FromDays(365) },
            { "P1M", Duration.FromDays(30) },

            // Fractional seconds.
            { "PT0.5S", Duration.FromNanoseconds(500_000_000) },
            { "PT1.123456789S", Duration.FromSeconds(1) + Duration.FromNanoseconds(123_456_789) },

            // Negative overall.
            { "-P1D", -Duration.FromDays(1) },
            { "-PT30M", -Duration.FromMinutes(30) },
            { "-PT0.5S", -Duration.FromNanoseconds(500_000_000) },

            // Per-component negative.
            { "P-1D", -Duration.FromDays(1) },
            { "PT-1H", -Duration.FromHours(1) },

            // Double negative (cancels out).
            { "-P-1D", Duration.FromDays(1) },

            // Comma as decimal separator.
            { "PT1,5S", Duration.FromSeconds(1) + Duration.FromNanoseconds(500_000_000) }
        };
    }

    public static TheoryData<string> InvalidDurations()
    {
        return new TheoryData<string>
        {
            // Empty.
            { "" },

            // Missing P.
            { "T1H" },
            { "1D" },

            // P alone (no components).
            { "P" },

            // T without time components.
            { "PT" },
            { "P1DT" },

            // Invalid designator.
            { "P1X" },
            { "PT1X" },

            // Fractional on non-seconds.
            { "P1.5D" },
            { "PT1.5H" },
            { "PT1.5M" },

            // No digits before designator.
            { "PD" },
            { "PTH" },

            // Digits without designator at end.
            { "PT5" },

            // Random text.
            { "hello" },

            // Leading plus sign (not supported).
            { "+PT1H" }
        };
    }

    public static TheoryData<Duration, string> OutputCases()
    {
        return new TheoryData<Duration, string>
        {
            // Zero.
            { Duration.Zero, "PT0S" },

            // Days only.
            { Duration.FromDays(1), "P1D" },
            { Duration.FromDays(7), "P7D" },
            { Duration.FromDays(365), "P365D" },

            // Time only.
            { Duration.FromHours(1), "PT1H" },
            { Duration.FromMinutes(30), "PT30M" },
            { Duration.FromSeconds(1), "PT1S" },

            // Composite.
            { Duration.FromDays(1) + Duration.FromHours(2) + Duration.FromMinutes(30), "P1DT2H30M" },

            // Fractional seconds.
            { Duration.FromNanoseconds(500_000_000), "PT0.5S" },
            { Duration.FromSeconds(1) + Duration.FromNanoseconds(123_456_789), "PT1.123456789S" },
            { Duration.FromNanoseconds(1), "PT0.000000001S" },

            // Negative.
            { -Duration.FromDays(1), "-P1D" },
            { -Duration.FromMinutes(30), "-PT30M" },
            { -Duration.FromNanoseconds(500_000_000), "-PT0.5S" },

            // Full decomposition.
            {
                Duration.FromDays(1)
                    + Duration.FromHours(1)
                    + Duration.FromMinutes(1)
                    + Duration.FromSeconds(1)
                    + Duration.FromNanoseconds(500_000_000),
                "P1DT1H1M1.5S"
            }
        };
    }

    private static JsonElement ParseInputValue(string sourceText)
    {
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(sourceText));
        return JsonElement.ParseValue(ref reader);
    }
}
