using System.Text;
using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;

namespace HotChocolate.Types.NodaTime;

public sealed class DurationTypeTests
{
    [Fact]
    public void Ensure_Type_Name_Is_Correct()
    {
        // arrange & act
        var type = new DurationType();

        // assert
        Assert.Equal("Duration", type.Name);
    }

    [Fact]
    public void CoerceInputLiteral()
    {
        // arrange
        var type = new DurationType();
        var literal = new StringValueNode("PT5M");
        var expectedDuration = Duration.FromMinutes(5);

        // act
        var runtimeValue = type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal(expectedDuration, runtimeValue);
    }

    [Fact]
    public void CoerceInputLiteral_MaxValue()
    {
        // arrange
        var type = new DurationType();
        var literal = new StringValueNode("P16777215DT23H59M59.999999999S");

        // act
        var runtimeValue = type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal(Duration.MaxValue, runtimeValue);
    }

    [Fact]
    public void CoerceInputLiteral_MinValue()
    {
        // arrange
        var type = new DurationType();
        var literal = new StringValueNode("-P16777216D");

        // act
        var runtimeValue = type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal(Duration.MinValue, runtimeValue);
    }

    [Fact]
    public void CoerceInputLiteral_Invalid_Format()
    {
        // arrange
        var type = new DurationType();
        var literal = new StringValueNode("bad");

        // act
        void Action() => type.CoerceInputLiteral(literal);

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }

    [Fact]
    public void CoerceInputValue()
    {
        // arrange
        var type = new DurationType();
        var inputValue = ParseInputValue("\"PT5M\"");
        var expectedDuration = Duration.FromMinutes(5);

        // act
        var runtimeValue = type.CoerceInputValue(inputValue, null!);

        // assert
        Assert.Equal(expectedDuration, runtimeValue);
    }

    [Fact]
    public void CoerceInputValue_Invalid_Format()
    {
        // arrange
        var type = new DurationType();
        var inputValue = ParseInputValue("\"bad\"");

        // act
        void Action() => type.CoerceInputValue(inputValue, null!);

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }

    [Fact]
    public void CoerceOutputValue()
    {
        // arrange
        var type = new DurationType();
        var runtimeValue = Duration.FromMinutes(5);

        // act
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        type.CoerceOutputValue(runtimeValue, resultValue);

        // assert
        Assert.Equal("PT5M", resultValue.GetString());
    }

    [Fact]
    public void CoerceOutputValue_Invalid_Format()
    {
        // arrange
        var type = new DurationType();

        // act
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        void Action() => type.CoerceOutputValue("bad", resultValue);

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }

    [Fact]
    public void ValueToLiteral()
    {
        // arrange
        var type = new DurationType();
        var runtimeValue = Duration.FromMinutes(5);

        // act
        var literal = type.ValueToLiteral(runtimeValue);

        // assert
        Assert.Equal("PT5M", Assert.IsType<StringValueNode>(literal).Value);
    }

    [Fact]
    public void ValueToLiteral_MaxValue()
    {
        // arrange
        var type = new DurationType();
        var runtimeValue = Duration.MaxValue;

        // act
        var literal = type.ValueToLiteral(runtimeValue);

        // assert
        Assert.Equal(
            "P16777215DT23H59M59.999999999S",
            Assert.IsType<StringValueNode>(literal).Value);
    }

    [Fact]
    public void ValueToLiteral_MinValue()
    {
        // arrange
        var type = new DurationType();
        var runtimeValue = Duration.MinValue;

        // act
        var literal = type.ValueToLiteral(runtimeValue);

        // assert
        Assert.Equal(
            "-P16777216D",
            Assert.IsType<StringValueNode>(literal).Value);
    }

    [Fact]
    public void ParseLiteral()
    {
        // arrange
        var type = new DurationType();
        var literal = new StringValueNode("PT5M");
        var expectedDuration = Duration.FromMinutes(5);

        // act
        var runtimeValue = type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal(expectedDuration, Assert.IsType<Duration>(runtimeValue));
    }

    [Fact]
    public void ParseLiteral_InvalidValue()
    {
        // arrange
        var type = new DurationType();

        // act
        void Action() => type.CoerceInputLiteral(new IntValueNode(123));

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }

    [Fact]
    public void Ensure_TypeKind_Is_Scalar()
    {
        // arrange
        var type = new DurationType();

        // act
        var kind = type.Kind;

        // assert
        Assert.Equal(TypeKind.Scalar, kind);
    }

    [Fact]
    public async Task Integration_SingleRuntimeType()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(b => b.Name(OperationTypeNames.Query))
            .AddType(typeof(QuerySingleRuntimeType))
            .AddNodaTime()
            .BuildRequestExecutorAsync();

        // act
        var result =
            await executor.ExecuteAsync(
                """{ duration(input: "P16777215DT23H59M59.999999999S") }""");

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "duration": "P16777215DT23H59M59.999999999S"
              }
            }
            """);
    }

    [QueryType]
    private static class QuerySingleRuntimeType
    {
        public static Duration GetDuration(Duration input) => input;
    }

    private static JsonElement ParseInputValue(string sourceText)
    {
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(sourceText));
        return JsonElement.ParseValue(ref reader);
    }
}
