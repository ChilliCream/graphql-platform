using System.Globalization;
using System.Text;
using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Text.Json;
using NodaTime;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime.Tests;

public class OffsetTimeTypeIntegrationTests
{
    [Fact]
    public void CoerceInputLiteral()
    {
        var type = new OffsetTimeType();
        var inputValue = new StringValueNode("18:30:13+02");
        var runtimeValue = type.CoerceInputLiteral(inputValue);
        Assert.Equal(
            new OffsetTime(new LocalTime(18, 30, 13), Offset.FromHours(2)),
            Assert.IsType<OffsetTime>(runtimeValue));
    }

    [Fact]
    public void CoerceInputLiteral_Invalid_Value_Throws()
    {
        var type = new OffsetTimeType();
        var valueLiteral = new StringValueNode("18:30:13");
        Action error = () => type.CoerceInputLiteral(valueLiteral);
        Assert.Throws<LeafCoercionException>(error);
    }

    [Fact]
    public void CoerceInputValue()
    {
        var type = new OffsetTimeType();
        var inputValue = ParseInputValue("\"18:30:13+02\"");
        var runtimeValue = type.CoerceInputValue(inputValue, null!);
        Assert.Equal(
            new OffsetTime(new LocalTime(18, 30, 13), Offset.FromHours(2)),
            Assert.IsType<OffsetTime>(runtimeValue));
    }

    [Fact]
    public void CoerceInputValue_Invalid_Value_Throws()
    {
        var type = new OffsetTimeType();
        var inputValue = ParseInputValue("\"18:30:13\"");
        Action error = () => type.CoerceInputValue(inputValue, null!);
        Assert.Throws<LeafCoercionException>(error);
    }

    [Fact]
    public void CoerceOutputValue()
    {
        var type = new OffsetTimeType();
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        type.CoerceOutputValue(new OffsetTime(new LocalTime(18, 30, 13), Offset.FromHours(2)), resultValue);
        Assert.Equal("18:30:13+02", resultValue.GetString());
    }

    [Fact]
    public void CoerceOutputValue_Invalid_Value_Throws()
    {
        var type = new OffsetTimeType();
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        Action error = () => type.CoerceOutputValue("18:30:13+02", resultValue);
        Assert.Throws<LeafCoercionException>(error);
    }

    [Fact]
    public void ValueToLiteral()
    {
        var type = new OffsetTimeType();
        var valueLiteral = type.ValueToLiteral(new OffsetTime(new LocalTime(18, 30, 13), Offset.FromHours(2)));
        Assert.Equal("\"18:30:13+02\"", valueLiteral.ToString());
    }

    [Fact]
    public void ValueToLiteral_Invalid_Value_Throws()
    {
        var type = new OffsetTimeType();
        Action error = () => type.ValueToLiteral("18:30:13+02");
        Assert.Throws<LeafCoercionException>(error);
    }

    [Fact]
    public void PatternEmptyThrowSchemaException()
    {
        static object Call() => new OffsetTimeType([]);
        Assert.Throws<SchemaException>(Call);
    }

    [Fact]
    public void OffsetTimeType_DescriptionKnownPatterns_MatchesSnapshot()
    {
        var offsetTimeType = new OffsetTimeType(
            OffsetTimePattern.GeneralIso,
            OffsetTimePattern.ExtendedIso);

        offsetTimeType.Description.MatchInlineSnapshot(
            """
            A combination of a LocalTime and an Offset, to represent a time-of-day at a specific offset from UTC but without any date information.

            Allowed patterns:
            - `hh:mm:ss±hh:mm`
            - `hh:mm:ss.sssssssss±hh:mm`

            Examples:
            - `20:00:00Z`
            - `20:00:00.999Z`
            """);
    }

    [Fact]
    public void OffsetTimeType_DescriptionUnknownPatterns_MatchesSnapshot()
    {
        var offsetTimeType = new OffsetTimeType(
            OffsetTimePattern.Create("mm", CultureInfo.InvariantCulture, new OffsetTime()));

        offsetTimeType.Description.MatchInlineSnapshot(
            "A combination of a LocalTime and an Offset, to represent a time-of-day at a specific offset from UTC but without any date information.");
    }

    private static JsonElement ParseInputValue(string sourceText)
    {
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(sourceText));
        return JsonElement.ParseValue(ref reader);
    }
}
