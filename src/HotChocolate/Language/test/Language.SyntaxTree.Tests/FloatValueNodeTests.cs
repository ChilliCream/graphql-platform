using System.Text;
using Xunit;

namespace HotChocolate.Language.SyntaxTree;

public class FloatValueNodeTests
{
    [InlineData("1.568")]
    [InlineData("2.0")]
    [Theory]
    public void CreateFloatValue(string value)
    {
        // arrange
        var buffer = Encoding.UTF8.GetBytes(value);

        // act
        var floatValueNode = new FloatValueNode(
            buffer, FloatFormat.FixedPoint);

        // assert
        Assert.Equal(value, floatValueNode.Value);
        Assert.Equal(SyntaxKind.FloatValue, floatValueNode.Kind);
        Assert.Null(floatValueNode.Location);
    }

    [InlineData("1.568")]
    [InlineData("2.0")]
    [Theory]
    public void CreateFloatValueWithLocation(string value)
    {
        // arrange
        var buffer = Encoding.UTF8.GetBytes(value);
        var location = new Location(0, 0, 0, 0);

        // act
        var floatValueNode = new FloatValueNode(
            location, buffer, FloatFormat.FixedPoint);

        // assert
        Assert.Equal(value, floatValueNode.Value);
        Assert.Equal(SyntaxKind.FloatValue, floatValueNode.Kind);
        Assert.Equal(location, floatValueNode.Location);
    }

    [InlineData("1.568", 1.568)]
    [InlineData("2.0", 2.0)]
    [Theory]
    public void ToSingle(string value, float expected)
    {
        // arrange
        var buffer = Encoding.UTF8.GetBytes(value);
        var location = new Location(0, 0, 0, 0);

        // act
        var floatValueNode = new FloatValueNode(
            location, buffer, FloatFormat.FixedPoint);

        // assert
        Assert.Equal(expected, floatValueNode.ToSingle());
    }

    [InlineData("1.568", 1.568)]
    [InlineData("2.0", 2.0)]
    [Theory]
    public void ToDouble(string value, double expected)
    {
        // arrange
        var buffer = Encoding.UTF8.GetBytes(value);
        var location = new Location(0, 0, 0, 0);

        // act
        var floatValueNode = new FloatValueNode(
            location, buffer, FloatFormat.FixedPoint);

        // assert
        Assert.Equal(expected, floatValueNode.ToDouble());
    }

    [InlineData("1.568", 1.568)]
    [InlineData("2.0", 2.0)]
    [Theory]
    public void ToDecimal(string value, decimal expected)
    {
        // arrange
        var buffer = Encoding.UTF8.GetBytes(value);
        var location = new Location(0, 0, 0, 0);

        // act
        var floatValueNode = new FloatValueNode(
            location, buffer, FloatFormat.FixedPoint);

        // assert
        Assert.Equal(expected, floatValueNode.ToDecimal());
    }

    [Fact]
    public void EqualsFloatValueNode_Float()
    {
        // arrange
        var a = new FloatValueNode((float)1.0);
        var b = new FloatValueNode((float)1.0);
        var c = new FloatValueNode((float)3.0);

        // act
        var abResult = SyntaxComparer.BySyntax.Equals(a, b);
        var aaResult = SyntaxComparer.BySyntax.Equals(a, a);
        var acResult = SyntaxComparer.BySyntax.Equals(a, c);
        var aNullResult = SyntaxComparer.BySyntax.Equals(a, default);

        // assert
        Assert.True(abResult);
        Assert.True(aaResult);
        Assert.False(acResult);
        Assert.False(aNullResult);
    }

    [Fact]
    public void EqualsFloatValueNode_Double()
    {
        // arrange
        var a = new FloatValueNode((double)1.0);
        var b = new FloatValueNode((double)1.0);
        var c = new FloatValueNode((double)3.0);

        // act
        var abResult = SyntaxComparer.BySyntax.Equals(a, b);
        var aaResult = SyntaxComparer.BySyntax.Equals(a, a);
        var acResult = SyntaxComparer.BySyntax.Equals(a, c);
        var aNullResult = SyntaxComparer.BySyntax.Equals(a, default);

        // assert
        Assert.True(abResult);
        Assert.True(aaResult);
        Assert.False(acResult);
        Assert.False(aNullResult);
    }

    [Fact]
    public void EqualsFloatValueNode_Decimal()
    {
        // arrange
        var a = new FloatValueNode((decimal)1.0);
        var b = new FloatValueNode((decimal)1.0);
        var c = new FloatValueNode((decimal)3.0);

        // act
        var abResult = SyntaxComparer.BySyntax.Equals(a, b);
        var aaResult = SyntaxComparer.BySyntax.Equals(a, a);
        var acResult = SyntaxComparer.BySyntax.Equals(a, c);
        var aNullResult = SyntaxComparer.BySyntax.Equals(a, default);

        // assert
        Assert.True(abResult);
        Assert.True(aaResult);
        Assert.False(acResult);
        Assert.False(aNullResult);
    }

    [Fact]
    public void EqualsIValueNode()
    {
        // arrange
        var a = new FloatValueNode(1.0);
        var b = new FloatValueNode(1.0);
        var c = new FloatValueNode(2.0);
        var d = new StringValueNode("foo");

        // act
        var abResult = SyntaxComparer.BySyntax.Equals(a, b);
        var aaResult = SyntaxComparer.BySyntax.Equals(a, a);
        var acResult = SyntaxComparer.BySyntax.Equals(a, c);
        var adResult = SyntaxComparer.BySyntax.Equals(a, d);
        var aNullResult = SyntaxComparer.BySyntax.Equals(a, default);

        // assert
        Assert.True(abResult);
        Assert.True(aaResult);
        Assert.False(acResult);
        Assert.False(adResult);
        Assert.False(aNullResult);
    }

    [Fact]
    public void CompareGetHashCode()
    {
        // arrange
        var a = new FloatValueNode(1.0);
        var b = new FloatValueNode(1.0);
        var c = new FloatValueNode(2.0);

        // act
        var aHash = SyntaxComparer.BySyntax.GetHashCode(a);
        var bHash = SyntaxComparer.BySyntax.GetHashCode(b);
        var cHash = SyntaxComparer.BySyntax.GetHashCode(c);

        // assert
        Assert.Equal(aHash, bHash);
        Assert.NotEqual(aHash, cHash);
    }

    [Fact]
    public void StringRepresentation()
    {
        // arrange
        var a = new FloatValueNode(1.0);
        var b = new FloatValueNode(2.0);

        // act
        var aString = a.ToString();
        var bString = b.ToString();

        // assert
        Assert.Equal("1", aString);
        Assert.Equal("2", bString);
    }

    [Fact]
    public void ClassIsSealed()
    {
        Assert.True(typeof(FloatValueNode).IsSealed);
    }

    [Fact]
    public void Convert_Value_Float_To_Span_To_String()
    {
        // act
        var a = new FloatValueNode(2.5);
        var b = a.WithValue(a.AsSpan(), FloatFormat.FixedPoint);
        var c = b.Value;

        // assert
        Assert.Equal("2.5", c);
    }

    [Fact]
    public void Equals_With_Same_Location()
    {
        var a = new FloatValueNode(
            TestLocations.Location1,
            1.1);
        var b = new FloatValueNode(
            TestLocations.Location1,
            1.1);
        var c = new FloatValueNode(
            TestLocations.Location1,
            1.2);

        // act
        var abResult = SyntaxComparer.BySyntax.Equals(a, b);
        var aaResult = SyntaxComparer.BySyntax.Equals(a, a);
        var acResult = SyntaxComparer.BySyntax.Equals(a, c);
        var aNullResult = SyntaxComparer.BySyntax.Equals(a, default);

        // assert
        Assert.True(abResult);
        Assert.True(aaResult);
        Assert.False(acResult);
        Assert.False(aNullResult);
    }

    [Fact]
    public void Equals_With_Different_Location()
    {
        // arrange
        var a = new FloatValueNode(
            TestLocations.Location1,
            1.1);
        var b = new FloatValueNode(
            TestLocations.Location2,
            1.1);
        var c = new FloatValueNode(
            TestLocations.Location1,
            1.2);

        // act
        var abResult = SyntaxComparer.BySyntax.Equals(a, b);
        var aaResult = SyntaxComparer.BySyntax.Equals(a, a);
        var acResult = SyntaxComparer.BySyntax.Equals(a, c);
        var aNullResult = SyntaxComparer.BySyntax.Equals(a, default);

        // assert
        Assert.True(abResult);
        Assert.True(aaResult);
        Assert.False(acResult);
        Assert.False(aNullResult);
    }

    [Fact]
    public void GetHashCode_With_Location()
    {
        // arrange
        var a = new FloatValueNode(
            TestLocations.Location1,
            1.1);
        var b = new FloatValueNode(
            TestLocations.Location2,
            1.1);
        var c = new FloatValueNode(
            TestLocations.Location1,
            1.2);
        var d = new FloatValueNode(
            TestLocations.Location2,
            1.2);

        // act
        var aHash = SyntaxComparer.BySyntax.GetHashCode(a);
        var bHash = SyntaxComparer.BySyntax.GetHashCode(b);
        var cHash = SyntaxComparer.BySyntax.GetHashCode(c);
        var dHash = SyntaxComparer.BySyntax.GetHashCode(d);

        // assert
        Assert.Equal(aHash, bHash);
        Assert.NotEqual(aHash, cHash);
        Assert.Equal(cHash, dHash);
        Assert.NotEqual(aHash, dHash);
    }
}
