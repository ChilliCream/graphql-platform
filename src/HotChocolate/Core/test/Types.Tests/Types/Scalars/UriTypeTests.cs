using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Text.Json;

namespace HotChocolate.Types;

public class UriTypeTests
{
    [Fact]
    public void Ensure_Type_Name_Is_Correct()
    {
        // arrange
        // act
        var type = new UriType();

        // assert
        Assert.Equal("URI", type.Name);
    }

    [Fact]
    public void EnsureUriTypeKindIsCorrect()
    {
        // arrange
        var type = new UriType();

        // act
        // assert
        Assert.Equal(TypeKind.Scalar, type.Kind);
    }

    [Fact]
    public void CoerceInputLiteral()
    {
        // arrange
        var type = new UriType();
        var expected = new Uri("http://domain.test/uri");
        var literal = new StringValueNode(expected.AbsoluteUri);

        // act
        var runtimeValue = type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal(expected, runtimeValue);
    }

    [Fact]
    public void CoerceInputLiteral_RelativeUri()
    {
        // arrange
        var type = new UriType();
        var expected = new Uri("/relative/path", UriKind.Relative);
        var literal = new StringValueNode(expected.ToString());

        // act
        var runtimeValue = type.CoerceInputLiteral(literal);

        // Assert
        Assert.Equal(expected, runtimeValue);
    }

    [Fact]
    public void CoerceInputValue()
    {
        // arrange
        var type = new UriType();
        var expected = new Uri("http://domain.test/uri");
        var inputValue = JsonDocument.Parse($"\"{expected.AbsoluteUri}\"").RootElement;

        // act
        var runtimeValue = type.CoerceInputValue(inputValue, null!);

        // assert
        Assert.Equal(expected, runtimeValue);
    }

    [Fact]
    public void CoerceInputValue_RelativeUri()
    {
        // arrange
        var type = new UriType();
        var expected = new Uri("/relative/path", UriKind.Relative);
        var inputValue = JsonDocument.Parse($"\"{expected}\"").RootElement;

        // act
        var runtimeValue = type.CoerceInputValue(inputValue, null!);

        // Assert
        Assert.Equal(expected, runtimeValue);
    }

    [Fact]
    public void CoerceOutputValue()
    {
        // arrange
        var type = new UriType();
        var uri = new Uri("http://domain.test/uri");

        // act
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        type.CoerceOutputValue(uri, resultValue);

        // assert
        resultValue.MatchInlineSnapshot("\"http://domain.test/uri\"");
    }

    [Fact]
    public void CoerceOutputValue_RelativeUri()
    {
        // arrange
        var type = new UriType();
        var uri = new Uri("/relative/path", UriKind.Relative);

        // act
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        type.CoerceOutputValue(uri, resultValue);

        // assert
        resultValue.MatchInlineSnapshot("\"/relative/path\"");
    }

    [Fact]
    public void CoerceOutputValue_Invalid_Format()
    {
        // arrange
        var type = new UriType();
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
        var type = new UriType();
        var uri = new Uri("http://domain.test/uri");
        var expectedLiteralValue = uri.AbsoluteUri;

        // act
        var literal = type.ValueToLiteral(uri);

        // assert
        Assert.Equal(expectedLiteralValue, Assert.IsType<StringValueNode>(literal).Value);
    }

    [Fact]
    public void ValueToLiteral_Encoded()
    {
        // arrange
        var type = new UriType();
        var uri = new Uri("http://domain.test/ä+😄?q=a/α");
        var expectedLiteralValue = uri.AbsoluteUri;

        // act
        var literal = type.ValueToLiteral(uri);

        // assert
        Assert.Equal(expectedLiteralValue, Assert.IsType<StringValueNode>(literal).Value);
    }

    [Fact]
    public void ValueToLiteral_RelativeUri()
    {
        // arrange
        var type = new UriType();
        var uri = new Uri("/relative/path", UriKind.Relative);

        // act
        var literal = type.ValueToLiteral(uri);

        // assert
        Assert.Equal(uri.ToString(), Assert.IsType<StringValueNode>(literal).Value);
    }

    [Fact]
    public void ParseLiteral()
    {
        // arrange
        var type = new UriType();
        var expected = new Uri("http://domain.test/uri");
        var literal = new StringValueNode(expected.AbsoluteUri);

        // act
        var runtimeValue = type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal(expected, Assert.IsType<Uri>(runtimeValue));
    }

    [Fact]
    public void ParseLiteral_InvalidValue()
    {
        // arrange
        var type = new UriType();

        // act
        void Action() => type.CoerceInputLiteral(new IntValueNode(123));

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }

    [Fact]
    public void IsValueCompatible_Uri_ReturnsTrue()
    {
        // Arrange
        var type = new UriType();
        var uri = new Uri("http://domain.test/uri");

        // Act
        var isCompatible = type.IsValueCompatible(new StringValueNode(uri.AbsoluteUri));

        // Assert
        Assert.True(isCompatible);
    }

    [Fact]
    public void IsValueCompatible_NullValueNode_ReturnsFalse()
    {
        // arrange
        var type = new UriType();

        // act
        var isCompatible = type.IsValueCompatible(NullValueNode.Default);

        // assert
        Assert.False(isCompatible);
    }

    [Fact]
    public void IsValueCompatible_Null_ReturnsFalse()
    {
        // arrange
        var type = new UriType();

        // act
        void Error() => type.IsValueCompatible(null!);

        // assert
        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void IsValueCompatible_IntValueNode_ReturnsFalse()
    {
        // arrange
        var type = new UriType();
        var intValue = new IntValueNode(1);

        // act
        var isCompatible = type.IsValueCompatible(intValue);

        // assert
        Assert.False(isCompatible);
    }
}
