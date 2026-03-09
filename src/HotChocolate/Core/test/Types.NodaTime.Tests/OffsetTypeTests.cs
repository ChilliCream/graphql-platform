using System.Globalization;
using System.Text;
using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Text.Json;
using NodaTime;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime.Tests;

public class OffsetTypeIntegrationTests
{
    [Fact]
    public void CoerceInputLiteral()
    {
        var type = new OffsetType();
        var inputValue = new StringValueNode("+02");
        var runtimeValue = type.CoerceInputLiteral(inputValue);
        Assert.Equal(Offset.FromHours(2), Assert.IsType<Offset>(runtimeValue));
    }

    [Fact]
    public void CoerceInputLiteral_With_Z()
    {
        var type = new OffsetType();
        var inputValue = new StringValueNode("Z");
        var runtimeValue = type.CoerceInputLiteral(inputValue);
        Assert.Equal(Offset.Zero, Assert.IsType<Offset>(runtimeValue));
    }

    [Fact]
    public void CoerceInputLiteral_Invalid_Value_Throws()
    {
        var type = new OffsetType();
        var valueLiteral = new StringValueNode("18:30:13+02");
        Action error = () => type.CoerceInputLiteral(valueLiteral);
        Assert.Throws<LeafCoercionException>(error);
    }

    [Fact]
    public void CoerceInputValue()
    {
        var type = new OffsetType();
        var inputValue = ParseInputValue("\"+02\"");
        var runtimeValue = type.CoerceInputValue(inputValue, null!);
        Assert.Equal(Offset.FromHours(2), Assert.IsType<Offset>(runtimeValue));
    }

    [Fact]
    public void CoerceInputValue_Invalid_Value_Throws()
    {
        var type = new OffsetType();
        var inputValue = ParseInputValue("\"18:30:13+02\"");
        Action error = () => type.CoerceInputValue(inputValue, null!);
        Assert.Throws<LeafCoercionException>(error);
    }

    [Fact]
    public void CoerceOutputValue()
    {
        var type = new OffsetType();
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        type.CoerceOutputValue(Offset.FromHours(2), resultValue);
        Assert.Equal("+02", resultValue.GetString());
    }

    [Fact]
    public void CoerceOutputValue_Zero()
    {
        var type = new OffsetType();
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        type.CoerceOutputValue(Offset.Zero, resultValue);
        Assert.Equal("Z", resultValue.GetString());
    }

    [Fact]
    public void CoerceOutputValue_Invalid_Value_Throws()
    {
        var type = new OffsetType();
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        Action error = () => type.CoerceOutputValue("+02", resultValue);
        Assert.Throws<LeafCoercionException>(error);
    }

    [Fact]
    public void ValueToLiteral()
    {
        var type = new OffsetType();
        var valueLiteral = type.ValueToLiteral(Offset.FromHours(2));
        Assert.Equal("\"+02\"", valueLiteral.ToString());
    }

    [Fact]
    public void ValueToLiteral_Invalid_Value_Throws()
    {
        var type = new OffsetType();
        Action error = () => type.ValueToLiteral("+02");
        Assert.Throws<LeafCoercionException>(error);
    }

    [Fact]
    public void PatternEmptyThrowSchemaException()
    {
        static object Call() => new OffsetType([]);
        Assert.Throws<SchemaException>(Call);
    }

    [Fact]
    public void OffsetType_DescriptionKnownPatterns_MatchesSnapshot()
    {
        var offsetType = new OffsetType(
            OffsetPattern.GeneralInvariant,
            OffsetPattern.GeneralInvariantWithZ);

        offsetType.Description.MatchInlineSnapshot(
            """
            An offset from UTC in seconds.
            A positive value means that the local time is ahead of UTC (e.g. for Europe); a negative value means that the local time is behind UTC (e.g. for America).

            Allowed patterns:
            - `±hh:mm:ss`
            - `Z`

            Examples:
            - `+02:30:00`
            - `Z`
            """);
    }

    [Fact]
    public void OffsetType_DescriptionUnknownPatterns_MatchesSnapshot()
    {
        var offsetType = new OffsetType(
            OffsetPattern.Create("mm", CultureInfo.InvariantCulture));

        offsetType.Description.MatchInlineSnapshot(
            """
            An offset from UTC in seconds.
            A positive value means that the local time is ahead of UTC (e.g. for Europe); a negative value means that the local time is behind UTC (e.g. for America).
            """);
    }

    private static JsonElement ParseInputValue(string sourceText)
    {
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(sourceText));
        return JsonElement.ParseValue(ref reader);
    }
}
