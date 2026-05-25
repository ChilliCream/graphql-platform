using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Text.Json;

namespace HotChocolate.Types;

public class UuidTypeTests
{
    [Fact]
    public void Ensure_Type_Name_Is_Correct()
    {
        // arrange
        // act
        var type = new UuidType();

        // assert
        Assert.Equal("UUID", type.Name);
    }

    [Fact]
    public void IsValueCompatible_StringLiteral()
    {
        // arrange
        var type = new UuidType();
        var guid = Guid.NewGuid();
        var literal = new StringValueNode(guid.ToString("D"));

        // act
        var isCompatible = type.IsValueCompatible(literal);

        // assert
        Assert.True(isCompatible);
    }

    [Fact]
    public void IsValueCompatible_NullLiteral()
    {
        // arrange
        var type = new UuidType();
        var literal = new NullValueNode(null);

        // act
        var isCompatible = type.IsValueCompatible(literal);

        // assert
        Assert.False(isCompatible);
    }

    [Fact]
    public void IsValueCompatible_IntLiteral()
    {
        // arrange
        var type = new UuidType();
        var literal = new IntValueNode(123);

        // act
        var isCompatible = type.IsValueCompatible(literal);

        // assert
        Assert.False(isCompatible);
    }

    [Fact]
    public void IsValueCompatible_Null()
    {
        // arrange
        var type = new UuidType();

        // act
        void Error() => type.IsValueCompatible(null!);

        // assert
        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void CoerceInputLiteral()
    {
        // arrange
        var type = new UuidType();
        var expected = Guid.NewGuid();
        var literal = new StringValueNode(expected.ToString("D"));

        // act
        var runtimeValue = type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal(expected, runtimeValue);
    }

    [Fact]
    public void CoerceInputLiteral_MultipleFormats()
    {
        // arrange
        var type = new UuidType();
        var expected = Guid.NewGuid();
        var literalN = new StringValueNode(expected.ToString("N"));
        var literalP = new StringValueNode(expected.ToString("P"));

        // act
        var runtimeValueN = (Guid)type.CoerceInputLiteral(literalN)!;
        var runtimeValueP = (Guid)type.CoerceInputLiteral(literalP)!;

        // assert
        Assert.Equal(expected, runtimeValueN);
        Assert.Equal(expected, runtimeValueP);
    }

    [Fact]
    public void CoerceInputLiteral_EnforceFormat()
    {
        // arrange
        var type = new UuidType(defaultFormat: 'P', enforceFormat: true);
        var expected = Guid.NewGuid();
        var literal = new StringValueNode(expected.ToString("N"));

        // act
        void Action() => type.CoerceInputLiteral(literal);

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }

    [Fact]
    public void CoerceInputLiteral_Invalid_Format()
    {
        // arrange
        var type = new UuidType();
        var literal = new IntValueNode(123);

        // act
        void Action() => type.CoerceInputLiteral(literal);

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }

    [Fact]
    public void CoerceInputLiteral_Invalid_Null()
    {
        // arrange
        var type = new UuidType();

        // act
        void Action() => type.CoerceInputLiteral(null!);

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }

    [Fact]
    public void CoerceInputValue()
    {
        // arrange
        var type = new UuidType();
        var guid = Guid.NewGuid();
        var inputValue = JsonDocument.Parse($"\"{guid:D}\"").RootElement;

        // act
        var runtimeValue = type.CoerceInputValue(inputValue, null!);

        // assert
        Assert.Equal(guid, runtimeValue);
    }

    [Theory]
    [InlineData('N')]
    [InlineData('D')]
    [InlineData('B')]
    [InlineData('P')]
    public void CoerceInputValue_WithFormat(char format)
    {
        // arrange
        var type = new UuidType(defaultFormat: format);
        var guid = Guid.Empty;
        var inputValue = JsonDocument.Parse($"\"{guid.ToString(format.ToString())}\"").RootElement;

        // act
        var runtimeValue = type.CoerceInputValue(inputValue, null!);

        // assert
        Assert.Equal(guid, runtimeValue);
    }

    [Fact]
    public void CoerceInputValue_Invalid_Format()
    {
        // arrange
        var type = new UuidType();
        var inputValue = JsonDocument.Parse("\"invalid\"").RootElement;

        // act
        void Action() => type.CoerceInputValue(inputValue, null!);

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }

    [Fact]
    public void CoerceOutputValue()
    {
        // arrange
        var type = new UuidType();
        var guid = Guid.Parse("c8c483be-4319-4903-a064-8a6ba66da99e");

        // act
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        type.CoerceOutputValue(guid, resultValue);

        // assert
        resultValue.MatchInlineSnapshot("\"c8c483be-4319-4903-a064-8a6ba66da99e\"");
    }

    [Fact]
    public void CoerceOutputValue_Invalid_Format()
    {
        // arrange
        var type = new UuidType();
        const int value = 123;

        // act
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        void Action() => type.CoerceOutputValue(value, resultValue);

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }

    [Fact]
    public void ValueToLiteral()
    {
        // arrange
        var type = new UuidType();
        var guid = Guid.NewGuid();
        var expectedLiteralValue = guid.ToString("D");

        // act
        var literal = type.ValueToLiteral(guid);

        // assert
        Assert.Equal(expectedLiteralValue, Assert.IsType<StringValueNode>(literal).Value);
    }

    [Theory]
    [InlineData('N')]
    [InlineData('D')]
    [InlineData('B')]
    [InlineData('P')]
    public void ValueToLiteral_WithFormat(char format)
    {
        // arrange
        var type = new UuidType(defaultFormat: format);
        var guid = Guid.Empty;

        // act
        var literal = type.ValueToLiteral(guid);

        // assert
        Assert.Equal(guid.ToString(format.ToString()), Assert.IsType<StringValueNode>(literal).Value);
    }

    [Fact]
    public void ParseLiteral()
    {
        // arrange
        var type = new UuidType();
        var expected = Guid.NewGuid();
        var literal = new StringValueNode(expected.ToString("D"));

        // act
        var runtimeValue = type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal(expected, Assert.IsType<Guid>(runtimeValue));
    }

    [Fact]
    public void ParseLiteral_InvalidValue()
    {
        // arrange
        var type = new UuidType();

        // act
        void Action() => type.CoerceInputLiteral(new IntValueNode(123));

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }

    [Fact]
    public void EnsureUuidTypeKindIsCorrect()
    {
        // arrange
        var type = new UuidType();

        // assert
        Assert.Equal(TypeKind.Scalar, type.Kind);
    }

    [Fact]
    public void Specify_Invalid_Format()
    {
        // arrange
        // act
        void Action() => new UuidType(defaultFormat: 'z');

        // assert
        Assert.Throws<ArgumentException>(Action).Message.MatchSnapshot();
    }

    [InlineData(false)]
    [InlineData(true)]
    [Theory]
    public void CoerceInputLiteral_Guid_String_With_Appended_String(bool enforceFormat)
    {
        // arrange
        var input = new StringValueNode("fbdef721-93c5-4267-8f92-ca27b60aa51f-foobar");
        var type = new UuidType(defaultFormat: 'D', enforceFormat: enforceFormat);

        // act
        void Fail() => type.CoerceInputLiteral(input);

        // assert
        Assert.Throws<LeafCoercionException>(Fail);
    }

    [InlineData(false)]
    [InlineData(true)]
    [Theory]
    public void CoerceInputLiteral_Guid_Valid_Input(bool enforceFormat)
    {
        // arrange
        var input = new StringValueNode("fbdef721-93c5-4267-8f92-ca27b60aa51f");
        var type = new UuidType(defaultFormat: 'D', enforceFormat: enforceFormat);

        // act
        var guid = (Guid)type.CoerceInputLiteral(input)!;

        // assert
        Assert.Equal(input.Value, guid.ToString("D"));
    }

    [InlineData(false)]
    [InlineData(true)]
    [Theory]
    public void CoerceInputValue_Guid_String_With_Appended_String(bool enforceFormat)
    {
        // arrange
        var inputValue = JsonDocument.Parse("\"fbdef721-93c5-4267-8f92-ca27b60aa51f-foobar\"").RootElement;
        var type = new UuidType(defaultFormat: 'D', enforceFormat: enforceFormat);

        // act
        void Fail() => type.CoerceInputValue(inputValue, null!);

        // assert
        Assert.Throws<LeafCoercionException>(Fail);
    }

    [InlineData(false)]
    [InlineData(true)]
    [Theory]
    public void CoerceInputValue_Guid_Valid_Format(bool enforceFormat)
    {
        // arrange
        const string input = "fbdef721-93c5-4267-8f92-ca27b60aa51f";
        var inputValue = JsonDocument.Parse($"\"{input}\"").RootElement;
        var type = new UuidType(defaultFormat: 'D', enforceFormat: enforceFormat);

        // act
        var guid = (Guid)type.CoerceInputValue(inputValue, null!)!;

        // assert
        Assert.Equal(input, guid.ToString("D"));
    }
}
