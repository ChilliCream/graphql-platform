using System.Globalization;
using System.Text;
using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Text.Json;
using NodaTime;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime.Tests;

public class InstantTypeIntegrationTests
{
    [Fact]
    public void CoerceInputLiteral()
    {
        var type = new InstantType();
        var inputValue = new StringValueNode("2020-02-20T17:42:59.000001234Z");
        var runtimeValue = type.CoerceInputLiteral(inputValue);
        Assert.Equal(
            Instant.FromUtc(2020, 02, 20, 17, 42, 59).PlusNanoseconds(1234),
            Assert.IsType<Instant>(runtimeValue));
    }

    [Fact]
    public void CoerceInputLiteral_Invalid_Value_Throws()
    {
        var type = new InstantType();
        var valueLiteral = new StringValueNode("2020-02-20T17:42:59");
        Action error = () => type.CoerceInputLiteral(valueLiteral);
        Assert.Throws<LeafCoercionException>(error);
    }

    [Fact]
    public void CoerceInputValue()
    {
        var type = new InstantType();
        var inputValue = ParseInputValue("\"2020-02-20T17:42:59.000001234Z\"");
        var runtimeValue = type.CoerceInputValue(inputValue, null!);
        Assert.Equal(
            Instant.FromUtc(2020, 02, 20, 17, 42, 59).PlusNanoseconds(1234),
            Assert.IsType<Instant>(runtimeValue));
    }

    [Fact]
    public void CoerceInputValue_Invalid_Value_Throws()
    {
        var type = new InstantType();
        var inputValue = ParseInputValue("\"2020-02-20T17:42:59\"");
        Action error = () => type.CoerceInputValue(inputValue, null!);
        Assert.Throws<LeafCoercionException>(error);
    }

    [Fact]
    public void CoerceOutputValue()
    {
        var type = new InstantType();
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        type.CoerceOutputValue(Instant.FromUtc(2020, 02, 20, 17, 42, 59).PlusNanoseconds(1234), resultValue);
        Assert.Equal("2020-02-20T17:42:59.000001234Z", resultValue.GetString());
    }

    [Fact]
    public void CoerceOutputValue_Invalid_Value_Throws()
    {
        var type = new InstantType();
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        Action error = () => type.CoerceOutputValue("2020-02-20T17:42:59.000001234Z", resultValue);
        Assert.Throws<LeafCoercionException>(error);
    }

    [Fact]
    public void ValueToLiteral()
    {
        var type = new InstantType();
        var valueLiteral = type.ValueToLiteral(Instant.FromUtc(2020, 02, 20, 17, 42, 59).PlusNanoseconds(1234));
        Assert.Equal("\"2020-02-20T17:42:59.000001234Z\"", valueLiteral.ToString());
    }

    [Fact]
    public void ValueToLiteral_Invalid_Value_Throws()
    {
        var type = new InstantType();
        Action error = () => type.ValueToLiteral("2020-02-20T17:42:59.000001234Z");
        Assert.Throws<LeafCoercionException>(error);
    }

    [Fact]
    public void PatternEmpty_ThrowSchemaException()
    {
        static object Call() => new InstantType([]);
        Assert.Throws<SchemaException>(Call);
    }

    [Fact]
    public void InstantType_DescriptionKnownPatterns_MatchesSnapshot()
    {
        var instantType = new InstantType(InstantPattern.General, InstantPattern.ExtendedIso);

        instantType.Description.MatchInlineSnapshot(
            """
            Represents an instant on the global timeline, with nanosecond resolution.

            Allowed patterns:
            - `YYYY-MM-DDThh:mm:ss±hh:mm`
            - `YYYY-MM-DDThh:mm:ss.sssssssss±hh:mm`

            Examples:
            - `2000-01-01T20:00:00Z`
            - `2000-01-01T20:00:00.999999999Z`
            """);
    }

    [Fact]
    public void InstantType_DescriptionUnknownPatterns_MatchesSnapshot()
    {
        var instantType = new InstantType(
            InstantPattern.Create("MM", CultureInfo.InvariantCulture));

        instantType.Description.MatchInlineSnapshot(
            "Represents an instant on the global timeline, with nanosecond resolution.");
    }

    private static JsonElement ParseInputValue(string sourceText)
    {
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(sourceText));
        return JsonElement.ParseValue(ref reader);
    }
}
