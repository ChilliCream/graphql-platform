using System.Text;
using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Text.Json;
using NodaTime;

namespace HotChocolate.Types.NodaTime.Tests;

public class PeriodTypeIntegrationTests
{
    [Fact]
    public void CoerceInputLiteral()
    {
        var type = new PeriodType();
        var inputValue = new StringValueNode("P-3W15DT139t");
        var runtimeValue = type.CoerceInputLiteral(inputValue);
        Assert.Equal(
            Period.FromWeeks(-3) + Period.FromDays(15) + Period.FromTicks(139),
            Assert.IsType<Period>(runtimeValue));
    }

    [Fact]
    public void CoerceInputLiteral_Invalid_Value_Throws()
    {
        var type = new PeriodType();
        var valueLiteral = new StringValueNode("-3W3DT-10M139t");
        Action error = () => type.CoerceInputLiteral(valueLiteral);
        Assert.Throws<LeafCoercionException>(error);
    }

    [Fact]
    public void CoerceInputValue()
    {
        var type = new PeriodType();
        var inputValue = ParseInputValue("\"P-3W15DT139t\"");
        var runtimeValue = type.CoerceInputValue(inputValue, null!);
        Assert.Equal(
            Period.FromWeeks(-3) + Period.FromDays(15) + Period.FromTicks(139),
            Assert.IsType<Period>(runtimeValue));
    }

    [Fact]
    public void CoerceInputValue_Invalid_Value_Throws()
    {
        var type = new PeriodType();
        var inputValue = ParseInputValue("\"-3W3DT-10M139t\"");
        Action error = () => type.CoerceInputValue(inputValue, null!);
        Assert.Throws<LeafCoercionException>(error);
    }

    [Fact]
    public void CoerceOutputValue()
    {
        var type = new PeriodType();
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        type.CoerceOutputValue(Period.FromWeeks(-3) + Period.FromDays(3) + Period.FromTicks(139), resultValue);
        Assert.Equal("P-3W3DT139t", resultValue.GetString());
    }

    [Fact]
    public void CoerceOutputValue_Invalid_Value_Throws()
    {
        var type = new PeriodType();
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        Action error = () => type.CoerceOutputValue("P-3W3DT139t", resultValue);
        Assert.Throws<LeafCoercionException>(error);
    }

    [Fact]
    public void ValueToLiteral()
    {
        var type = new PeriodType();
        var valueLiteral = type.ValueToLiteral(Period.FromWeeks(-3) + Period.FromDays(3) + Period.FromTicks(139));
        Assert.Equal("\"P-3W3DT139t\"", valueLiteral.ToString());
    }

    [Fact]
    public void ValueToLiteral_Invalid_Value_Throws()
    {
        var type = new PeriodType();
        Action error = () => type.ValueToLiteral("P-3W3DT139t");
        Assert.Throws<LeafCoercionException>(error);
    }

    [Fact]
    public void PatternEmptyThrowSchemaException()
    {
        static object Call() => new PeriodType([]);
        Assert.Throws<SchemaException>(Call);
    }

    private static JsonElement ParseInputValue(string sourceText)
    {
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(sourceText));
        return JsonElement.ParseValue(ref reader);
    }
}
