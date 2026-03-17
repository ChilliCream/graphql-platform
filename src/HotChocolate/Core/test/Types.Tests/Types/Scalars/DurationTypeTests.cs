using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Text.Json;
using HotChocolate.Types.Descriptors;
using System.Reflection;

namespace HotChocolate.Types;

public class DurationTypeTests
{
    [Fact]
    public void Ensure_Type_Name_Is_Correct()
    {
        // arrange
        // act
        var type = new DurationType();

        // assert
        Assert.Equal("Duration", type.Name);
    }

    [Fact]
    public void CoerceInputLiteral()
    {
        // arrange
        var type = new DurationType();
        var literal = new StringValueNode("PT5M");
        var expectedTimeSpan = TimeSpan.FromMinutes(5);

        // act
        var runtimeValue = type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal(expectedTimeSpan, runtimeValue);
    }

    [Theory]
    [InlineData(DurationFormat.Iso8601, "PT5M")]
    [InlineData(DurationFormat.DotNet, "00:05:00")]
    public void CoerceInputLiteral_WithFormat(DurationFormat format, string literalValue)
    {
        // arrange
        var type = new DurationType(format);
        var literal = new StringValueNode(literalValue);
        var expectedTimeSpan = TimeSpan.FromMinutes(5);

        // act
        var runtimeValue = type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal(expectedTimeSpan, runtimeValue);
    }

    [Theory]
    [InlineData(DurationFormat.Iso8601, "P10675199DT2H48M5.4775807S")]
    [InlineData(DurationFormat.DotNet, "10675199.02:48:05.4775807")]
    public void CoerceInputLiteral_MaxValue(DurationFormat format, string literalValue)
    {
        // arrange
        var type = new DurationType(format);
        var literal = new StringValueNode(literalValue);

        // act
        var runtimeValue = type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal(TimeSpan.MaxValue, runtimeValue);
    }

    [Theory]
    [InlineData(DurationFormat.Iso8601, "-P10675199DT2H48M5.4775808S")]
    [InlineData(DurationFormat.DotNet, "-10675199.02:48:05.4775808")]
    public void CoerceInputLiteral_MinValue(DurationFormat format, string literalValue)
    {
        // arrange
        var type = new DurationType(format);
        var literal = new StringValueNode(literalValue);

        // act
        var runtimeValue = type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal(TimeSpan.MinValue, runtimeValue);
    }

    [Fact]
    public void CoerceInputLiteral_Weeks()
    {
        // arrange
        var type = new DurationType();
        var literal = new StringValueNode("P2M2W5D");
        var expectedTimeSpan = TimeSpan.FromDays(79);

        // act
        var runtimeValue = type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal(expectedTimeSpan, runtimeValue);
    }

    [Fact]
    public void CoerceInputLiteral_Invalid_Format()
    {
        // arrange
        var type = new DurationType();
        var literal = new StringValueNode("bad");

        // act
        void Action() => type.CoerceInputLiteral(literal);

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }

    [Fact]
    public void CoerceInputLiteral_CannotEndWithDigits()
    {
        // arrange
        var type = new DurationType();
        var literal = new StringValueNode("PT5");

        // act
        void Action() => type.CoerceInputLiteral(literal);

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }

    [Fact]
    public void CoerceInputValue()
    {
        // arrange
        var type = new DurationType();
        var inputValue = JsonDocument.Parse("\"PT5M\"").RootElement;
        var expectedTimeSpan = TimeSpan.FromMinutes(5);

        // act
        var runtimeValue = type.CoerceInputValue(inputValue, null!);

        // assert
        Assert.Equal(expectedTimeSpan, runtimeValue);
    }

    [Theory]
    [InlineData(DurationFormat.Iso8601, "PT5M")]
    [InlineData(DurationFormat.DotNet, "00:05:00")]
    public void CoerceInputValue_WithFormat(DurationFormat format, string value)
    {
        // arrange
        var type = new DurationType(format);
        var inputValue = JsonDocument.Parse($"\"{value}\"").RootElement;
        var expectedTimeSpan = TimeSpan.FromMinutes(5);

        // act
        var runtimeValue = type.CoerceInputValue(inputValue, null!);

        // assert
        Assert.Equal(expectedTimeSpan, runtimeValue);
    }

    [Fact]
    public void CoerceInputValue_Invalid_Format()
    {
        // arrange
        var type = new DurationType();
        var inputValue = JsonDocument.Parse("\"bad\"").RootElement;

        // act
        void Action() => type.CoerceInputValue(inputValue, null!);

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }

    [Fact]
    public void CoerceOutputValue()
    {
        // arrange
        var type = new DurationType();
        var runtimeValue = TimeSpan.FromMinutes(5);

        // act
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        type.CoerceOutputValue(runtimeValue, resultValue);

        // assert
        resultValue.MatchInlineSnapshot("\"PT5M\"");
    }

    [Fact]
    public void CoerceOutputValue_WithFormat_Iso8601()
    {
        // arrange
        var type = new DurationType(DurationFormat.Iso8601);
        var runtimeValue = TimeSpan.FromMinutes(5);

        // act
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        type.CoerceOutputValue(runtimeValue, resultValue);

        // assert
        resultValue.MatchInlineSnapshot("\"PT5M\"");
    }

    [Fact]
    public void CoerceOutputValue_WithFormat_DotNet()
    {
        // arrange
        var type = new DurationType(DurationFormat.DotNet);
        var runtimeValue = TimeSpan.FromMinutes(5);

        // act
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        type.CoerceOutputValue(runtimeValue, resultValue);

        // assert
        resultValue.MatchInlineSnapshot("\"00:05:00\"");
    }

    [Fact]
    public void CoerceOutputValue_Invalid_Format()
    {
        // arrange
        var type = new DurationType();

        // act
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        void Action() => type.CoerceOutputValue("bad", resultValue);

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }

    [Fact]
    public void ValueToLiteral()
    {
        // arrange
        var type = new DurationType();
        var runtimeValue = TimeSpan.FromMinutes(5);

        // act
        var literal = type.ValueToLiteral(runtimeValue);

        // assert
        Assert.Equal("PT5M", Assert.IsType<StringValueNode>(literal).Value);
    }

    [Theory]
    [InlineData(DurationFormat.Iso8601, "PT5M")]
    [InlineData(DurationFormat.DotNet, "00:05:00")]
    public void ValueToLiteral_WithFormat(DurationFormat format, string expectedValue)
    {
        // arrange
        var type = new DurationType(format);
        var runtimeValue = TimeSpan.FromMinutes(5);

        // act
        var literal = type.ValueToLiteral(runtimeValue);

        // assert
        Assert.Equal(expectedValue, Assert.IsType<StringValueNode>(literal).Value);
    }

    [Theory]
    [InlineData(DurationFormat.Iso8601, "P10675199DT2H48M5.4775807S")]
    [InlineData(DurationFormat.DotNet, "10675199.02:48:05.4775807")]
    public void ValueToLiteral_MaxValue(DurationFormat format, string expectedValue)
    {
        // arrange
        var type = new DurationType(format);
        var runtimeValue = TimeSpan.MaxValue;

        // act
        var literal = type.ValueToLiteral(runtimeValue);

        // assert
        Assert.Equal(expectedValue, Assert.IsType<StringValueNode>(literal).Value);
    }

    [Theory]
    [InlineData(DurationFormat.Iso8601, "-P10675199DT2H48M5.4775808S")]
    [InlineData(DurationFormat.DotNet, "-10675199.02:48:05.4775808")]
    public void ValueToLiteral_MinValue(DurationFormat format, string expectedValue)
    {
        // arrange
        var type = new DurationType(format);
        var runtimeValue = TimeSpan.MinValue;

        // act
        var literal = type.ValueToLiteral(runtimeValue);

        // assert
        Assert.Equal(expectedValue, Assert.IsType<StringValueNode>(literal).Value);
    }

    [Fact]
    public void ParseLiteral()
    {
        // arrange
        var type = new DurationType();
        var literal = new StringValueNode("PT5M");
        var expectedTimeSpan = TimeSpan.FromMinutes(5);

        // act
        var runtimeValue = type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal(expectedTimeSpan, Assert.IsType<TimeSpan>(runtimeValue));
    }

    [Fact]
    public void ParseLiteral_InvalidValue()
    {
        // arrange
        var type = new DurationType();

        // act
        void Action() => type.CoerceInputLiteral(new IntValueNode(123));

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }

    // ----------------------------------------------------------------
    // Tests mirrored from NodaTime DurationTypeTests to ensure
    // API-layer parity between the two Duration scalar implementations.
    // ----------------------------------------------------------------

    [Fact]
    public void CoerceInputLiteral_Zero()
    {
        var type = new DurationType();
        var literal = new StringValueNode("PT0S");

        var result = type.CoerceInputLiteral(literal);

        Assert.Equal(TimeSpan.Zero, result);
    }

    [Fact]
    public void CoerceInputValue_Zero()
    {
        var type = new DurationType();
        var inputValue = JsonDocument.Parse("\"PT0S\"").RootElement;

        var result = type.CoerceInputValue(inputValue, null!);

        Assert.Equal(TimeSpan.Zero, result);
    }

    [Fact]
    public void CoerceOutputValue_Zero()
    {
        var type = new DurationType();
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");

        type.CoerceOutputValue(TimeSpan.Zero, resultValue);

        Assert.Equal("PT0S", resultValue.GetString());
    }

    [Fact]
    public void CoerceOutputValue_Invalid_Type_Throws()
    {
        var type = new DurationType();
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");

        Assert.Throws<LeafCoercionException>(() => type.CoerceOutputValue("P1D", resultValue));
    }

    [Fact]
    public void ValueToLiteral_Zero()
    {
        var type = new DurationType();

        var literal = type.ValueToLiteral(TimeSpan.Zero);

        Assert.Equal("PT0S", Assert.IsType<StringValueNode>(literal).Value);
    }

    [Fact]
    public void ValueToLiteral_Invalid_Type_Throws()
    {
        var type = new DurationType();
        Assert.Throws<LeafCoercionException>(() => type.ValueToLiteral("PT1H"));
    }

    [Theory]
    [InlineData("PT0S")]
    [InlineData("P1D")]
    [InlineData("P7D")]
    [InlineData("PT1H")]
    [InlineData("PT30M")]
    [InlineData("PT1S")]
    [InlineData("PT0.5S")]
    [InlineData("P1DT2H30M")]
    [InlineData("P365D")]
    [InlineData("-P1D")]
    [InlineData("-PT30M")]
    [InlineData("P1DT1H1M1.5S")]
    public void RoundTrip_Canonical(string iso)
    {
        var type = new DurationType();

        // Parse.
        var literal = new StringValueNode(iso);
        var timeSpan = (TimeSpan)type.CoerceInputLiteral(literal);

        // Format back to string.
        var output = Assert.IsType<StringValueNode>(type.ValueToLiteral(timeSpan));

        Assert.Equal(iso, output.Value);
    }

    [Theory]
    [MemberData(nameof(ValidIso8601Durations))]
    public void CoerceInputLiteral_Valid_Iso8601(string iso, TimeSpan expected)
    {
        var type = new DurationType();
        var literal = new StringValueNode(iso);

        var result = type.CoerceInputLiteral(literal);

        Assert.Equal(expected, result);
    }

    [Theory]
    [MemberData(nameof(ValidIso8601Durations))]
    public void CoerceInputValue_Valid_Iso8601(string iso, TimeSpan expected)
    {
        var type = new DurationType();
        var inputValue = JsonDocument.Parse($"\"{iso}\"").RootElement;

        var result = type.CoerceInputValue(inputValue, null!);

        Assert.Equal(expected, result);
    }

    [Theory]
    [MemberData(nameof(InvalidIso8601Durations))]
    public void CoerceInputLiteral_Invalid_Iso8601(string iso)
    {
        var type = new DurationType();
        var literal = new StringValueNode(iso);

        Assert.Throws<LeafCoercionException>(() => type.CoerceInputLiteral(literal));
    }

    [Theory]
    [MemberData(nameof(InvalidIso8601Durations))]
    public void CoerceInputValue_Invalid_Iso8601(string iso)
    {
        var type = new DurationType();
        var inputValue = JsonDocument.Parse($"\"{iso}\"").RootElement;

        Assert.Throws<LeafCoercionException>(() => type.CoerceInputValue(inputValue, null!));
    }

    [Theory]
    [MemberData(nameof(OutputIso8601Cases))]
    public void CoerceOutputValue_Valid_Iso8601(TimeSpan timeSpan, string expected)
    {
        var type = new DurationType();
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");

        type.CoerceOutputValue(timeSpan, resultValue);

        Assert.Equal(expected, resultValue.GetString());
    }

    [Theory]
    [MemberData(nameof(OutputIso8601Cases))]
    public void ValueToLiteral_Valid_Iso8601(TimeSpan timeSpan, string expected)
    {
        var type = new DurationType();

        var literal = type.ValueToLiteral(timeSpan);

        Assert.Equal(expected, Assert.IsType<StringValueNode>(literal).Value);
    }

    [Fact]
    public void CoerceInputLiteral_Months()
    {
        // "P1M" → 30 days (matches NodaTime DurationType behavior).
        var type = new DurationType();
        var literal = new StringValueNode("P1M");

        var result = type.CoerceInputLiteral(literal);

        Assert.Equal(TimeSpan.FromDays(30), result);
    }

    [Fact]
    public void CoerceInputLiteral_Weeks_Single()
    {
        // "P2W" → 14 days (matches NodaTime DurationType behavior).
        var type = new DurationType();
        var literal = new StringValueNode("P2W");

        var result = type.CoerceInputLiteral(literal);

        Assert.Equal(TimeSpan.FromDays(14), result);
    }

    [Fact]
    public void CoerceInputLiteral_AllComponents()
    {
        // "P1Y2M3W4DT5H6M7.5S" → 450d 5h 6m 7.5s
        // (matches NodaTime DurationType behavior).
        var type = new DurationType();
        var literal = new StringValueNode("P1Y2M3W4DT5H6M7.5S");

        var result = (TimeSpan)type.CoerceInputLiteral(literal);

        var expected = TimeSpan.FromDays(450)
            + TimeSpan.FromHours(5)
            + TimeSpan.FromMinutes(6)
            + TimeSpan.FromSeconds(7.5);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void CoerceInputLiteral_FractionalSeconds_1Digit()
    {
        var type = new DurationType();
        var literal = new StringValueNode("PT0.5S");

        var result = type.CoerceInputLiteral(literal);

        Assert.Equal(TimeSpan.FromSeconds(0.5), result);
    }

    [Fact]
    public void CoerceInputLiteral_FractionalSeconds_7Digits()
    {
        var type = new DurationType();
        var literal = new StringValueNode("PT1.1234567S");

        var result = type.CoerceInputLiteral(literal);

        Assert.Equal(
            TimeSpan.FromSeconds(1) + TimeSpan.FromTicks(1_234_567),
            result);
    }

    [Fact]
    public void CoerceInputLiteral_FractionalSeconds_ExcessDigitsTruncated()
    {
        // More than 7 fractional digits: excess digits are discarded
        // (TimeSpan has tick precision = 100ns = 7 decimal digits).
        var type = new DurationType();
        var literal = new StringValueNode("PT1.12345678S");

        var result = (TimeSpan)type.CoerceInputLiteral(literal);

        Assert.Equal(
            TimeSpan.FromSeconds(1) + TimeSpan.FromTicks(1_234_567),
            result);
    }

    public static TheoryData<string, TimeSpan> ValidIso8601Durations()
    {
        return new TheoryData<string, TimeSpan>
        {
            // Zero.
            { "PT0S", TimeSpan.Zero },
            { "P0D", TimeSpan.Zero },

            // Days.
            { "P1D", TimeSpan.FromDays(1) },
            { "P7D", TimeSpan.FromDays(7) },

            // Time components.
            { "PT1H", TimeSpan.FromHours(1) },
            { "PT30M", TimeSpan.FromMinutes(30) },
            { "PT1S", TimeSpan.FromSeconds(1) },

            // Composite.
            { "P1DT2H30M", TimeSpan.FromDays(1) + TimeSpan.FromHours(2) + TimeSpan.FromMinutes(30) },

            // Weeks.
            { "P2W", TimeSpan.FromDays(14) },

            // Months.
            { "P1M", TimeSpan.FromDays(30) },

            // Fractional seconds.
            { "PT0.5S", TimeSpan.FromSeconds(0.5) },

            // Negative overall.
            { "-P1D", -TimeSpan.FromDays(1) },
            { "-PT30M", -TimeSpan.FromMinutes(30) },
            { "-PT0.5S", -TimeSpan.FromSeconds(0.5) }
        };
    }

    public static TheoryData<string> InvalidIso8601Durations()
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

            // Invalid designator.
            { "P1X" },
            { "PT1X" },

            // No digits before designator.
            { "PD" },
            { "PTH" },

            // Digits without designator at end.
            { "PT5" },

            // Random text.
            { "hello" }
        };
    }

    public static TheoryData<TimeSpan, string> OutputIso8601Cases()
    {
        return new TheoryData<TimeSpan, string>
        {
            // Zero.
            { TimeSpan.Zero, "PT0S" },

            // Days only.
            { TimeSpan.FromDays(1), "P1D" },
            { TimeSpan.FromDays(7), "P7D" },
            { TimeSpan.FromDays(365), "P365D" },

            // Time only.
            { TimeSpan.FromHours(1), "PT1H" },
            { TimeSpan.FromMinutes(30), "PT30M" },
            { TimeSpan.FromSeconds(1), "PT1S" },

            // Composite.
            { TimeSpan.FromDays(1) + TimeSpan.FromHours(2) + TimeSpan.FromMinutes(30), "P1DT2H30M" },

            // Fractional seconds.
            { TimeSpan.FromSeconds(0.5), "PT0.5S" },

            // Negative.
            { -TimeSpan.FromDays(1), "-P1D" },
            { -TimeSpan.FromMinutes(30), "-PT30M" },
            { -TimeSpan.FromSeconds(0.5), "-PT0.5S" },

            // Full decomposition.
            {
                TimeSpan.FromDays(1)
                    + TimeSpan.FromHours(1)
                    + TimeSpan.FromMinutes(1)
                    + TimeSpan.FromSeconds(1.5),
                "P1DT1H1M1.5S"
            }
        };
    }

    [Fact]
    public void PureCodeFirst_AutomaticallyBinds_TimeSpan()
    {
        SchemaBuilder.New()
            .AddQueryType<Query>()
            .Create()
            .ToString()
            .MatchSnapshot();
    }

    [InlineData(DurationFormat.Iso8601)]
    [InlineData(DurationFormat.DotNet)]
    [Theory]
    public void PureCodeFirst_AutomaticallyBinds_TimeSpan_With_Format(
        DurationFormat format)
    {
        SchemaBuilder.New()
            .AddQueryType<Query>()
            .AddType(new DurationType(format: format))
            .Create()
            .MakeExecutable()
            .Execute("{ duration }")
            .ToJson()
            .MatchSnapshot(postFix: format);
    }

    [Fact]
    public void PureCodeFirst_Different_TimeSpan_Formats_In_Same_Type()
    {
        SchemaBuilder.New()
            .AddQueryType<QueryWithTwoDurations>()
            .AddType(new DurationType(format: DurationFormat.DotNet))
            .AddType(new DurationType(
                "IsoTimeSpan",
                format: DurationFormat.Iso8601,
                bind: BindingBehavior.Explicit))
            .Create()
            .MakeExecutable()
            .Execute("{ duration1 duration2 }")
            .ToJson()
            .MatchSnapshot();
    }

    public class Query
    {
        public TimeSpan Duration() => TimeSpan.FromDays(1);
    }

    public class QueryWithTwoDurations
    {
        public TimeSpan Duration1() => TimeSpan.FromDays(1);

        [IsoTimeSpan]
        public TimeSpan Duration2() => TimeSpan.FromDays(1);
    }

    private sealed class IsoTimeSpanAttribute : ObjectFieldDescriptorAttribute
    {
        protected override void OnConfigure(
            IDescriptorContext context,
            IObjectFieldDescriptor descriptor,
            MemberInfo? member)
        {
            descriptor.Extend().OnBeforeCreate(
                d => d.Type = new SyntaxTypeReference(
                    new NamedTypeNode("IsoTimeSpan"),
                    TypeContext.Output));
        }
    }
}
