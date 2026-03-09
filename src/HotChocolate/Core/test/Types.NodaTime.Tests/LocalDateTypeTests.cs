using System.Globalization;
using System.Text;
using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Text.Json;
using NodaTime;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime.Tests;

public class LocalDateTypeIntegrationTests
{
    [Fact]
    public void CoerceInputLiteral()
    {
        var type = new LocalDateType();
        var inputValue = new StringValueNode("2020-02-20");
        var runtimeValue = type.CoerceInputLiteral(inputValue);
        Assert.Equal(new LocalDate(2020, 2, 20), Assert.IsType<LocalDate>(runtimeValue));
    }

    [Fact]
    public void CoerceInputLiteral_Invalid_Value_Throws()
    {
        var type = new LocalDateType();
        var valueLiteral = new StringValueNode("2020-02-20T17:42:59");
        Action error = () => type.CoerceInputLiteral(valueLiteral);
        Assert.Throws<LeafCoercionException>(error);
    }

    [Fact]
    public void CoerceInputValue()
    {
        var type = new LocalDateType();
        var inputValue = ParseInputValue("\"2020-02-20\"");
        var runtimeValue = type.CoerceInputValue(inputValue, null!);
        Assert.Equal(new LocalDate(2020, 2, 20), Assert.IsType<LocalDate>(runtimeValue));
    }

    [Fact]
    public void CoerceInputValue_Invalid_Value_Throws()
    {
        var type = new LocalDateType();
        var inputValue = ParseInputValue("\"2020-02-20T17:42:59\"");
        Action error = () => type.CoerceInputValue(inputValue, null!);
        Assert.Throws<LeafCoercionException>(error);
    }

    [Fact]
    public void CoerceOutputValue()
    {
        var type = new LocalDateType();
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        type.CoerceOutputValue(new LocalDate(2020, 2, 20), resultValue);
        Assert.Equal("2020-02-20", resultValue.GetString());
    }

    [Fact]
    public void CoerceOutputValue_Invalid_Value_Throws()
    {
        var type = new LocalDateType();
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        Action error = () => type.CoerceOutputValue("2020-02-20", resultValue);
        Assert.Throws<LeafCoercionException>(error);
    }

    [Fact]
    public void ValueToLiteral()
    {
        var type = new LocalDateType();
        var valueLiteral = type.ValueToLiteral(new LocalDate(2020, 2, 20));
        Assert.Equal("\"2020-02-20\"", valueLiteral.ToString());
    }

    [Fact]
    public void ValueToLiteral_Invalid_Value_Throws()
    {
        var type = new LocalDateType();
        Action error = () => type.ValueToLiteral("2020-02-20");
        Assert.Throws<LeafCoercionException>(error);
    }

    [Fact]
    public void PatternEmpty_ThrowSchemaException()
    {
        static object Call() => new LocalDateType([]);
        Assert.Throws<SchemaException>(Call);
    }

    [Fact]
    public void LocalDateType_DescriptionKnownPatterns_MatchesSnapshot()
    {
        var localDateType = new LocalDateType(LocalDatePattern.Iso, LocalDatePattern.FullRoundtrip);

        localDateType.Description.MatchInlineSnapshot(
            """
            LocalDate represents a date within the calendar, with no reference to a particular time zone or time of day.

            Allowed patterns:
            - `YYYY-MM-DD`
            - `YYYY-MM-DD (calendar)`

            Examples:
            - `2000-01-01`
            - `2000-01-01 (ISO)`
            """);
    }

    [Fact]
    public void LocalDateType_DescriptionUnknownPatterns_MatchesSnapshot()
    {
        var localDateType = new LocalDateType(
            LocalDatePattern.Create("MM", CultureInfo.InvariantCulture));

        localDateType.Description.MatchInlineSnapshot(
            "LocalDate represents a date within the calendar, with no reference to a particular time zone or time of day.");
    }

    private static JsonElement ParseInputValue(string sourceText)
    {
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(sourceText));
        return JsonElement.ParseValue(ref reader);
    }
}
