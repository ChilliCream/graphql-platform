using System.Globalization;
using System.Text;
using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Text.Json;
using NodaTime;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime.Tests;

public class LocalDateTimeTypeIntegrationTests
{
    [Fact]
    public void CoerceInputLiteral()
    {
        var type = new LocalDateTimeType();
        var inputValue = new StringValueNode("2020-02-20T17:42:59.000001234");
        var runtimeValue = type.CoerceInputLiteral(inputValue);
        Assert.Equal(
            LocalDateTime.FromDateTime(new DateTime(2020, 02, 20, 17, 42, 59)).PlusNanoseconds(1234),
            Assert.IsType<LocalDateTime>(runtimeValue));
    }

    [Fact]
    public void CoerceInputLiteral_Invalid_Value_Throws()
    {
        var type = new LocalDateTimeType();
        var valueLiteral = new StringValueNode("2020-02-20T17:42:59.000001234Z");
        Action error = () => type.CoerceInputLiteral(valueLiteral);
        Assert.Throws<LeafCoercionException>(error);
    }

    [Fact]
    public void CoerceInputValue()
    {
        var type = new LocalDateTimeType();
        var inputValue = ParseInputValue("\"2020-02-20T17:42:59.000001234\"");
        var runtimeValue = type.CoerceInputValue(inputValue, null!);
        Assert.Equal(
            LocalDateTime.FromDateTime(new DateTime(2020, 02, 20, 17, 42, 59)).PlusNanoseconds(1234),
            Assert.IsType<LocalDateTime>(runtimeValue));
    }

    [Fact]
    public void CoerceInputValue_Invalid_Value_Throws()
    {
        var type = new LocalDateTimeType();
        var inputValue = ParseInputValue("\"2020-02-20T17:42:59.000001234Z\"");
        Action error = () => type.CoerceInputValue(inputValue, null!);
        Assert.Throws<LeafCoercionException>(error);
    }

    [Fact]
    public void CoerceOutputValue()
    {
        var type = new LocalDateTimeType();
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        type.CoerceOutputValue(LocalDateTime.FromDateTime(new DateTime(2020, 02, 20, 17, 42, 59)).PlusNanoseconds(1234), resultValue);
        Assert.Equal("2020-02-20T17:42:59.000001234", resultValue.GetString());
    }

    [Fact]
    public void CoerceOutputValue_Invalid_Value_Throws()
    {
        var type = new LocalDateTimeType();
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        Action error = () => type.CoerceOutputValue("2020-02-20T17:42:59.000001234", resultValue);
        Assert.Throws<LeafCoercionException>(error);
    }

    [Fact]
    public void ValueToLiteral()
    {
        var type = new LocalDateTimeType();
        var valueLiteral = type.ValueToLiteral(LocalDateTime.FromDateTime(new DateTime(2020, 02, 20, 17, 42, 59)).PlusNanoseconds(1234));
        Assert.Equal("\"2020-02-20T17:42:59.000001234\"", valueLiteral.ToString());
    }

    [Fact]
    public void ValueToLiteral_Invalid_Value_Throws()
    {
        var type = new LocalDateTimeType();
        Action error = () => type.ValueToLiteral("2020-02-20T17:42:59.000001234");
        Assert.Throws<LeafCoercionException>(error);
    }

    [Fact]
    public void PatternEmpty_ThrowSchemaException()
    {
        static object Call() => new LocalDateTimeType([]);
        Assert.Throws<SchemaException>(Call);
    }

    [Fact]
    public void LocalDateTimeType_DescriptionKnownPatterns_MatchesSnapshot()
    {
        var localDateTimeType = new LocalDateTimeType(
            LocalDateTimePattern.ExtendedIso,
            LocalDateTimePattern.FullRoundtrip);

        localDateTimeType.Description.MatchInlineSnapshot(
            """
            A date and time in a particular calendar system.

            Allowed patterns:
            - `YYYY-MM-DDThh:mm:ss.sssssssss`
            - `YYYY-MM-DDThh:mm:ss.sssssssss (calendar)`

            Examples:
            - `2000-01-01T20:00:00.999`
            - `2000-01-01T20:00:00.999999999 (ISO)`
            """);
    }

    [Fact]
    public void LocalDateTimeType_DescriptionUnknownPatterns_MatchesSnapshot()
    {
        var localDateTimeType = new LocalDateTimeType(
            LocalDateTimePattern.Create("MM", CultureInfo.InvariantCulture));

        localDateTimeType.Description.MatchInlineSnapshot(
            "A date and time in a particular calendar system.");
    }

    private static JsonElement ParseInputValue(string sourceText)
    {
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(sourceText));
        return JsonElement.ParseValue(ref reader);
    }
}
