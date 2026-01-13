using System.Globalization;
using System.Text;
using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Text.Json;
using NodaTime;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime.Tests;

public class OffsetDateTimeTypeIntegrationTests
{
    [Fact]
    public void CoerceInputLiteral()
    {
        var type = new OffsetDateTimeType();
        var inputValue = new StringValueNode("2020-12-31T18:30:13+02");
        var runtimeValue = type.CoerceInputLiteral(inputValue);
        Assert.Equal(
            new LocalDateTime(2020, 12, 31, 18, 30, 13).WithOffset(Offset.FromHours(2)),
            Assert.IsType<OffsetDateTime>(runtimeValue));
    }

    [Fact]
    public void CoerceInputLiteral_Invalid_Value_Throws()
    {
        var type = new OffsetDateTimeType();
        var valueLiteral = new StringValueNode("2020-12-31T18:30:13");
        Action error = () => type.CoerceInputLiteral(valueLiteral);
        Assert.Throws<LeafCoercionException>(error);
    }

    [Fact]
    public void CoerceInputValue()
    {
        var type = new OffsetDateTimeType();
        var inputValue = ParseInputValue("\"2020-12-31T18:30:13+02\"");
        var runtimeValue = type.CoerceInputValue(inputValue, null!);
        Assert.Equal(
            new LocalDateTime(2020, 12, 31, 18, 30, 13).WithOffset(Offset.FromHours(2)),
            Assert.IsType<OffsetDateTime>(runtimeValue));
    }

    [Fact]
    public void CoerceInputValue_Invalid_Value_Throws()
    {
        var type = new OffsetDateTimeType();
        var inputValue = ParseInputValue("\"2020-12-31T18:30:13\"");
        Action error = () => type.CoerceInputValue(inputValue, null!);
        Assert.Throws<LeafCoercionException>(error);
    }

    [Fact]
    public void CoerceOutputValue()
    {
        var type = new OffsetDateTimeType();
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        type.CoerceOutputValue(new LocalDateTime(2020, 12, 31, 18, 30, 13).WithOffset(Offset.FromHours(2)), resultValue);
        Assert.Equal("2020-12-31T18:30:13+02", resultValue.GetString());
    }

    [Fact]
    public void CoerceOutputValue_Invalid_Value_Throws()
    {
        var type = new OffsetDateTimeType();
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        Action error = () => type.CoerceOutputValue("2020-12-31T18:30:13+02", resultValue);
        Assert.Throws<LeafCoercionException>(error);
    }

    [Fact]
    public void ValueToLiteral()
    {
        var type = new OffsetDateTimeType();
        var valueLiteral = type.ValueToLiteral(new LocalDateTime(2020, 12, 31, 18, 30, 13).WithOffset(Offset.FromHours(2)));
        Assert.Equal("\"2020-12-31T18:30:13+02\"", valueLiteral.ToString());
    }

    [Fact]
    public void ValueToLiteral_Invalid_Value_Throws()
    {
        var type = new OffsetDateTimeType();
        Action error = () => type.ValueToLiteral("2020-12-31T18:30:13+02");
        Assert.Throws<LeafCoercionException>(error);
    }

    [Fact]
    public void PatternEmptyThrowSchemaException()
    {
        static object Call() => new OffsetDateTimeType([]);
        Assert.Throws<SchemaException>(Call);
    }

    [Fact]
    public void OffsetDateTimeType_DescriptionKnownPatterns_MatchesSnapshot()
    {
        var offsetDateTimeType = new OffsetDateTimeType(
            OffsetDateTimePattern.ExtendedIso,
            OffsetDateTimePattern.FullRoundtrip);

        offsetDateTimeType.Description.MatchInlineSnapshot(
            """
            A local date and time in a particular calendar system, combined with an offset from UTC.

            Allowed patterns:
            - `YYYY-MM-DDThh:mm:ss.sssssssss±hh:mm`
            - `YYYY-MM-DDThh:mm:ss.sssssssss±hh:mm (calendar)`

            Examples:
            - `2000-01-01T20:00:00.999Z`
            - `2000-01-01T20:00:00.999Z (ISO)`
            """);
    }

    [Fact]
    public void OffsetDateTimeType_DescriptionUnknownPatterns_MatchesSnapshot()
    {
        var offsetDateTimeType = new OffsetDateTimeType(
            OffsetDateTimePattern.Create("MM", CultureInfo.InvariantCulture, new OffsetDateTime()));

        offsetDateTimeType.Description.MatchInlineSnapshot(
            "A local date and time in a particular calendar system, combined with an offset from UTC.");
    }

    private static JsonElement ParseInputValue(string sourceText)
    {
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(sourceText));
        return JsonElement.ParseValue(ref reader);
    }
}
