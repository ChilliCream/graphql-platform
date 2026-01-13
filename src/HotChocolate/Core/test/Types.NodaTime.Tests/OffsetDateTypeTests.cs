using System.Globalization;
using System.Text;
using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Text.Json;
using NodaTime;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime.Tests;

public class OffsetDateTypeIntegrationTests
{
    [Fact]
    public void CoerceInputLiteral()
    {
        var type = new OffsetDateType();
        var inputValue = new StringValueNode("2020-12-31+02");
        var runtimeValue = type.CoerceInputLiteral(inputValue);
        Assert.Equal(
            new OffsetDate(new LocalDate(2020, 12, 31), Offset.FromHours(2)),
            Assert.IsType<OffsetDate>(runtimeValue));
    }

    [Fact]
    public void CoerceInputLiteral_Invalid_Value_Throws()
    {
        var type = new OffsetDateType();
        var valueLiteral = new StringValueNode("2020-12-31");
        Action error = () => type.CoerceInputLiteral(valueLiteral);
        Assert.Throws<LeafCoercionException>(error);
    }

    [Fact]
    public void CoerceInputValue()
    {
        var type = new OffsetDateType();
        var inputValue = ParseInputValue("\"2020-12-31+02\"");
        var runtimeValue = type.CoerceInputValue(inputValue, null!);
        Assert.Equal(
            new OffsetDate(new LocalDate(2020, 12, 31), Offset.FromHours(2)),
            Assert.IsType<OffsetDate>(runtimeValue));
    }

    [Fact]
    public void CoerceInputValue_Invalid_Value_Throws()
    {
        var type = new OffsetDateType();
        var inputValue = ParseInputValue("\"2020-12-31\"");
        Action error = () => type.CoerceInputValue(inputValue, null!);
        Assert.Throws<LeafCoercionException>(error);
    }

    [Fact]
    public void CoerceOutputValue()
    {
        var type = new OffsetDateType();
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        type.CoerceOutputValue(new OffsetDate(new LocalDate(2020, 12, 31), Offset.FromHours(2)), resultValue);
        Assert.Equal("2020-12-31+02", resultValue.GetString());
    }

    [Fact]
    public void CoerceOutputValue_Invalid_Value_Throws()
    {
        var type = new OffsetDateType();
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        Action error = () => type.CoerceOutputValue("2020-12-31+02", resultValue);
        Assert.Throws<LeafCoercionException>(error);
    }

    [Fact]
    public void ValueToLiteral()
    {
        var type = new OffsetDateType();
        var valueLiteral = type.ValueToLiteral(new OffsetDate(new LocalDate(2020, 12, 31), Offset.FromHours(2)));
        Assert.Equal("\"2020-12-31+02\"", valueLiteral.ToString());
    }

    [Fact]
    public void ValueToLiteral_Invalid_Value_Throws()
    {
        var type = new OffsetDateType();
        Action error = () => type.ValueToLiteral("2020-12-31+02");
        Assert.Throws<LeafCoercionException>(error);
    }

    [Fact]
    public void PatternEmptyThrowSchemaException()
    {
        static object Call() => new OffsetDateType([]);
        Assert.Throws<SchemaException>(Call);
    }

    [Fact]
    public void OffsetDateType_DescriptionKnownPatterns_MatchesSnapshot()
    {
        var offsetDateType = new OffsetDateType(
            OffsetDatePattern.GeneralIso,
            OffsetDatePattern.FullRoundtrip);

        offsetDateType.Description.MatchInlineSnapshot(
            """
            A combination of a LocalDate and an Offset, to represent a date at a specific offset from UTC but without any time-of-day information.

            Allowed patterns:
            - `YYYY-MM-DD±hh:mm`
            - `YYYY-MM-DD±hh:mm (calendar)`

            Examples:
            - `2000-01-01Z`
            - `2000-01-01Z (ISO)`
            """);
    }

    [Fact]
    public void OffsetDateType_DescriptionUnknownPatterns_MatchesSnapshot()
    {
        var offsetDateType = new OffsetDateType(
            OffsetDatePattern.Create("MM", CultureInfo.InvariantCulture, new OffsetDate()));

        offsetDateType.Description.MatchInlineSnapshot(
            "A combination of a LocalDate and an Offset, to represent a date at a specific offset from UTC but without any time-of-day information.");
    }

    private static JsonElement ParseInputValue(string sourceText)
    {
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(sourceText));
        return JsonElement.ParseValue(ref reader);
    }
}
