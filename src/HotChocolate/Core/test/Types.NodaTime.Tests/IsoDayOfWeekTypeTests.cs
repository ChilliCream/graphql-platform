using System.Text;
using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Text.Json;
using NodaTime;

namespace HotChocolate.Types.NodaTime.Tests;

public class IsoDayOfWeekTypeIntegrationTests
{
    [Fact]
    public void CoerceInputLiteral()
    {
        var type = new IsoDayOfWeekType();
        var inputValue = new IntValueNode(1);
        var runtimeValue = type.CoerceInputLiteral(inputValue);
        Assert.Equal(IsoDayOfWeek.Monday, Assert.IsType<IsoDayOfWeek>(runtimeValue));
    }

    [Fact]
    public void CoerceInputLiteral_Sunday()
    {
        var type = new IsoDayOfWeekType();
        var inputValue = new IntValueNode(7);
        var runtimeValue = type.CoerceInputLiteral(inputValue);
        Assert.Equal(IsoDayOfWeek.Sunday, Assert.IsType<IsoDayOfWeek>(runtimeValue));
    }

    [Fact]
    public void CoerceInputLiteral_Invalid_Zero_Throws()
    {
        var type = new IsoDayOfWeekType();
        var valueLiteral = new IntValueNode(0);
        Action error = () => type.CoerceInputLiteral(valueLiteral);
        Assert.Throws<LeafCoercionException>(error);
    }

    [Fact]
    public void CoerceInputLiteral_Invalid_Eight_Throws()
    {
        var type = new IsoDayOfWeekType();
        var valueLiteral = new IntValueNode(8);
        Action error = () => type.CoerceInputLiteral(valueLiteral);
        Assert.Throws<LeafCoercionException>(error);
    }

    [Fact]
    public void CoerceInputLiteral_Invalid_Negative_Throws()
    {
        var type = new IsoDayOfWeekType();
        var valueLiteral = new IntValueNode(-2);
        Action error = () => type.CoerceInputLiteral(valueLiteral);
        Assert.Throws<LeafCoercionException>(error);
    }

    [Fact]
    public void CoerceInputValue()
    {
        var type = new IsoDayOfWeekType();
        var inputValue = ParseInputValue("1");
        var runtimeValue = type.CoerceInputValue(inputValue, null!);
        Assert.Equal(IsoDayOfWeek.Monday, Assert.IsType<IsoDayOfWeek>(runtimeValue));
    }

    [Fact]
    public void CoerceInputValue_Invalid_Zero_Throws()
    {
        var type = new IsoDayOfWeekType();
        var inputValue = ParseInputValue("0");
        Action error = () => type.CoerceInputValue(inputValue, null!);
        Assert.Throws<LeafCoercionException>(error);
    }

    [Fact]
    public void CoerceOutputValue()
    {
        var type = new IsoDayOfWeekType();
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        type.CoerceOutputValue(IsoDayOfWeek.Monday, resultValue);
        Assert.Equal(1, resultValue.GetInt32());
    }

    [Fact]
    public void CoerceOutputValue_Sunday()
    {
        var type = new IsoDayOfWeekType();
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        type.CoerceOutputValue(IsoDayOfWeek.Sunday, resultValue);
        Assert.Equal(7, resultValue.GetInt32());
    }

    [Fact]
    public void CoerceOutputValue_Invalid_None_Throws()
    {
        var type = new IsoDayOfWeekType();
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        Action error = () => type.CoerceOutputValue(IsoDayOfWeek.None, resultValue);
        Assert.Throws<LeafCoercionException>(error);
    }

    [Fact]
    public void CoerceOutputValue_Invalid_Value_Throws()
    {
        var type = new IsoDayOfWeekType();
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        Action error = () => type.CoerceOutputValue(1, resultValue);
        Assert.Throws<LeafCoercionException>(error);
    }

    [Fact]
    public void ValueToLiteral()
    {
        var type = new IsoDayOfWeekType();
        var valueLiteral = type.ValueToLiteral(IsoDayOfWeek.Monday);
        Assert.Equal("1", valueLiteral.ToString());
    }

    [Fact]
    public void ValueToLiteral_Invalid_Value_Throws()
    {
        var type = new IsoDayOfWeekType();
        Action error = () => type.ValueToLiteral(1);
        Assert.Throws<LeafCoercionException>(error);
    }

    private static JsonElement ParseInputValue(string sourceText)
    {
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(sourceText));
        return JsonElement.ParseValue(ref reader);
    }
}
