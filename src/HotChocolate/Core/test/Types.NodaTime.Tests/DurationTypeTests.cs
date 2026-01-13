using System.Globalization;
using System.Text;
using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Text.Json;
using NodaTime;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime.Tests;

public class DurationTypeIntegrationTests
{
    [Fact]
    public void CoerceInputLiteral()
    {
        var type = new DurationType();
        var inputValue = new StringValueNode("123:07:53:10.019");
        var runtimeValue = type.CoerceInputLiteral(inputValue);
        Assert.Equal(
            Duration.FromTimeSpan(new TimeSpan(123, 7, 53, 10, 19)),
            Assert.IsType<Duration>(runtimeValue));
    }

    [Fact]
    public void CoerceInputLiteral_Invalid_Value_Throws()
    {
        var type = new DurationType();
        var valueLiteral = new StringValueNode("+09:22:01:00");
        Action error = () => type.CoerceInputLiteral(valueLiteral);
        Assert.Throws<LeafCoercionException>(error);
    }

    [Fact]
    public void CoerceInputValue()
    {
        var type = new DurationType();
        var inputValue = ParseInputValue("\"123:07:53:10.019\"");
        var runtimeValue = type.CoerceInputValue(inputValue, null!);
        Assert.Equal(
            Duration.FromTimeSpan(new TimeSpan(123, 7, 53, 10, 19)),
            Assert.IsType<Duration>(runtimeValue));
    }

    [Fact]
    public void CoerceInputValue_Invalid_Value_Throws()
    {
        var type = new DurationType();
        var inputValue = ParseInputValue("\"+09:22:01:00\"");
        Action error = () => type.CoerceInputValue(inputValue, null!);
        Assert.Throws<LeafCoercionException>(error);
    }

    [Fact]
    public void CoerceOutputValue()
    {
        var type = new DurationType();
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        type.CoerceOutputValue(Duration.FromTimeSpan(new TimeSpan(123, 7, 53, 10, 19)), resultValue);
        Assert.Equal("123:07:53:10.019", resultValue.GetString());
    }

    [Fact]
    public void CoerceOutputValue_Invalid_Value_Throws()
    {
        var type = new DurationType();
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        Action error = () => type.CoerceOutputValue("123:07:53:10.019", resultValue);
        Assert.Throws<LeafCoercionException>(error);
    }

    [Fact]
    public void ValueToLiteral()
    {
        var type = new DurationType();
        var valueLiteral = type.ValueToLiteral(Duration.FromTimeSpan(new TimeSpan(123, 7, 53, 10, 19)));
        Assert.Equal("\"123:07:53:10.019\"", valueLiteral.ToString());
    }

    [Fact]
    public void ValueToLiteral_Invalid_Value_Throws()
    {
        var type = new DurationType();
        Action error = () => type.ValueToLiteral("123:07:53:10.019");
        Assert.Throws<LeafCoercionException>(error);
    }

    [Fact]
    public void PatternEmpty_ThrowSchemaException()
    {
        static object Call() => new DurationType([]);
        Assert.Throws<SchemaException>(Call);
    }

    [Fact]
    public void DurationType_DescriptionKnownPatterns_MatchesSnapshot()
    {
        var durationType = new DurationType(
            DurationPattern.Roundtrip,
            DurationPattern.JsonRoundtrip);

        durationType.Description.MatchInlineSnapshot(
            """
            Represents a fixed (and calendar-independent) length of time.

            Allowed patterns:
            - `-D:hh:mm:ss.sssssssss`
            - `-hh:mm:ss.sssssssss`

            Examples:
            - `-1:20:00:00.999999999`
            - `-44:00:00.999999999`
            """);
    }

    [Fact]
    public void DurationType_DescriptionUnknownPatterns_MatchesSnapshot()
    {
        var durationType = new DurationType(
            DurationPattern.Create("mm", CultureInfo.InvariantCulture));

        durationType.Description.MatchInlineSnapshot(
            "Represents a fixed (and calendar-independent) length of time.");
    }

    private static JsonElement ParseInputValue(string sourceText)
    {
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(sourceText));
        return JsonElement.ParseValue(ref reader);
    }
}
