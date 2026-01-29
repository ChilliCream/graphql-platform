using System.Globalization;
using System.Text;
using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Text.Json;
using NodaTime;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime.Tests;

public class ZonedDateTimeTypeIntegrationTests
{
    [Fact]
    public void CoerceInputLiteral()
    {
        var type = new ZonedDateTimeType();
        var inputValue = new StringValueNode("2020-12-31T18:30:13 Asia/Kathmandu +05:45");
        var runtimeValue = type.CoerceInputLiteral(inputValue);
        Assert.Equal(
            new ZonedDateTime(
                LocalDateTime.FromDateTime(new DateTime(2020, 12, 31, 18, 30, 13)),
                DateTimeZoneProviders.Tzdb["Asia/Kathmandu"],
                Offset.FromHoursAndMinutes(5, 45)),
            Assert.IsType<ZonedDateTime>(runtimeValue));
    }

    [Fact]
    public void CoerceInputLiteral_Utc()
    {
        var type = new ZonedDateTimeType();
        var inputValue = new StringValueNode("2020-12-31T18:30:13 UTC +00");
        var runtimeValue = type.CoerceInputLiteral(inputValue);
        Assert.Equal(
            new ZonedDateTime(
                LocalDateTime.FromDateTime(new DateTime(2020, 12, 31, 18, 30, 13)),
                DateTimeZoneProviders.Tzdb["UTC"],
                Offset.FromHours(0)),
            Assert.IsType<ZonedDateTime>(runtimeValue));
    }

    [Fact]
    public void CoerceInputLiteral_Invalid_Value_Throws()
    {
        var type = new ZonedDateTimeType();
        var valueLiteral = new StringValueNode("2020-12-31T19:30:13 UTC");
        Action error = () => type.CoerceInputLiteral(valueLiteral);
        Assert.Throws<LeafCoercionException>(error);
    }

    [Fact]
    public void CoerceInputValue()
    {
        var type = new ZonedDateTimeType();
        var inputValue = ParseInputValue("\"2020-12-31T18:30:13 Asia/Kathmandu +05:45\"");
        var runtimeValue = type.CoerceInputValue(inputValue, null!);
        Assert.Equal(
            new ZonedDateTime(
                LocalDateTime.FromDateTime(new DateTime(2020, 12, 31, 18, 30, 13)),
                DateTimeZoneProviders.Tzdb["Asia/Kathmandu"],
                Offset.FromHoursAndMinutes(5, 45)),
            Assert.IsType<ZonedDateTime>(runtimeValue));
    }

    [Fact]
    public void CoerceInputValue_Invalid_Value_Throws()
    {
        var type = new ZonedDateTimeType();
        var inputValue = ParseInputValue("\"2020-12-31T19:30:13 UTC\"");
        Action error = () => type.CoerceInputValue(inputValue, null!);
        Assert.Throws<LeafCoercionException>(error);
    }

    [Fact]
    public void CoerceOutputValue()
    {
        var type = new ZonedDateTimeType();
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        type.CoerceOutputValue(
            new ZonedDateTime(
                LocalDateTime.FromDateTime(new DateTime(2020, 12, 31, 18, 30, 13)),
                DateTimeZoneProviders.Tzdb["UTC"],
                Offset.FromHours(0)),
            resultValue);
        Assert.Equal("2020-12-31T18:30:13 UTC +00", resultValue.GetString());
    }

    [Fact]
    public void CoerceOutputValue_Invalid_Value_Throws()
    {
        var type = new ZonedDateTimeType();
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        Action error = () => type.CoerceOutputValue("2020-12-31T18:30:13 UTC +00", resultValue);
        Assert.Throws<LeafCoercionException>(error);
    }

    [Fact]
    public void ValueToLiteral()
    {
        var type = new ZonedDateTimeType();
        var valueLiteral = type.ValueToLiteral(
            new ZonedDateTime(
                LocalDateTime.FromDateTime(new DateTime(2020, 12, 31, 18, 30, 13)),
                DateTimeZoneProviders.Tzdb["UTC"],
                Offset.FromHours(0)));
        Assert.Equal("\"2020-12-31T18:30:13 UTC +00\"", valueLiteral.ToString());
    }

    [Fact]
    public void ValueToLiteral_Invalid_Value_Throws()
    {
        var type = new ZonedDateTimeType();
        Action error = () => type.ValueToLiteral("2020-12-31T18:30:13 UTC +00");
        Assert.Throws<LeafCoercionException>(error);
    }

    [Fact]
    public void PatternEmpty_ThrowSchemaException()
    {
        static object Call() => new ZonedDateTimeType([]);
        Assert.Throws<SchemaException>(Call);
    }

    [Fact]
    public void ZonedDateTimeType_DescriptionKnownPatterns_MatchesSnapshot()
    {
        var zonedDateTimeType = new ZonedDateTimeType(
            ZonedDateTimePattern.GeneralFormatOnlyIso,
            ZonedDateTimePattern.ExtendedFormatOnlyIso);

        zonedDateTimeType.Description.MatchInlineSnapshot(
            """
            A LocalDateTime in a specific time zone and with a particular offset to distinguish between otherwise-ambiguous instants.
            A ZonedDateTime is global, in that it maps to a single Instant.

            Allowed patterns:
            - `YYYY-MM-DDThh:mm:ss z (±hh:mm)`
            - `YYYY-MM-DDThh:mm:ss.sssssssss z (±hh:mm)`

            Examples:
            - `2000-01-01T20:00:00 Europe/Zurich (+01)`
            - `2000-01-01T20:00:00.999999999 Europe/Zurich (+01)`
            """);
    }

    [Fact]
    public void ZonedDateTimeType_DescriptionUnknownPatterns_MatchesSnapshot()
    {
        var zonedDateTimeType = new ZonedDateTimeType(
            ZonedDateTimePattern.Create(
                "MM",
                CultureInfo.InvariantCulture,
                null,
                null,
                new ZonedDateTime()));

        zonedDateTimeType.Description.MatchInlineSnapshot(
            """
            A LocalDateTime in a specific time zone and with a particular offset to distinguish between otherwise-ambiguous instants.
            A ZonedDateTime is global, in that it maps to a single Instant.
            """);
    }

    private static JsonElement ParseInputValue(string sourceText)
    {
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(sourceText));
        return JsonElement.ParseValue(ref reader);
    }
}
