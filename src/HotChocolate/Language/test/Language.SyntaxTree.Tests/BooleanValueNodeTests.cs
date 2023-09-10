using Xunit;

namespace HotChocolate.Language.SyntaxTree;

public class BooleanValueNodeTests
{
    [InlineData(false)]
    [InlineData(true)]
    [Theory]
    public void CreateBooleanValue(bool value)
    {
        // act
        var booleanValueNode = new BooleanValueNode(value);

        // assert
        Assert.Equal(value, booleanValueNode.Value);
        Assert.Equal(SyntaxKind.BooleanValue, booleanValueNode.Kind);
        Assert.Null(booleanValueNode.Location);
    }

    [InlineData(false)]
    [InlineData(true)]
    [Theory]
    public void CreateBooleanValueWithLocation(bool value)
    {
        // arrange
        var location = new Location(0, 0, 0, 0);

        // act
        var booleanValueNode = new BooleanValueNode(location, value);

        // assert
        Assert.Equal(value, booleanValueNode.Value);
        Assert.Equal(SyntaxKind.BooleanValue, booleanValueNode.Kind);
        Assert.Equal(location, booleanValueNode.Location);
    }

    [Fact]
    public void EqualsBooleanValueNode_SameLocation()
    {
        // arrange
        var a = new BooleanValueNode(new Location(1, 1, 1, 1), false);
        var b = new BooleanValueNode(new Location(1, 1, 1, 1), false);
        var c = new BooleanValueNode(new Location(1, 1, 1, 1), true);

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
    public void EqualsBooleanValueNode_DifferentLocation()
    {
        // arrange
        var a = new BooleanValueNode(new Location(1, 1, 1, 1), false);
        var b = new BooleanValueNode(new Location(2, 2, 2, 2), false);
        var c = new BooleanValueNode(new Location(3, 3, 3, 3), true);

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
    public void EqualsBooleanValueNode()
    {
        // arrange
        var a = new BooleanValueNode(false);
        var b = new BooleanValueNode(false);
        var c = new BooleanValueNode(true);

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
    public void CompareGetHashCode()
    {
        // arrange
        var a = new BooleanValueNode(false);
        var b = new BooleanValueNode(false);
        var c = new BooleanValueNode(true);

        // act
        var aHash = SyntaxComparer.BySyntax.GetHashCode(a);
        var bHash = SyntaxComparer.BySyntax.GetHashCode(b);
        var cHash = SyntaxComparer.BySyntax.GetHashCode(c);

        // assert
        Assert.Equal(aHash, bHash);
        Assert.NotEqual(aHash, cHash);
    }

    [Fact]
    public void CompareGetHashCode_With_Location()
    {
        // arrange
        var a = new BooleanValueNode(new(1, 1, 1, 1), false);
        var b = new BooleanValueNode(new(2, 2, 2, 2), false);
        var c = new BooleanValueNode(new(1, 1, 1, 1), true);
        var d = new BooleanValueNode(new(2, 2, 2, 2), true);

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

    [Fact]
    public void StringRepresentation()
    {
        // arrange
        var a = new BooleanValueNode(false);
        var b = new BooleanValueNode(true);

        // act
        var aString = a.ToString();
        var bString = b.ToString();

        // assert
        Assert.Equal("false", aString);
        Assert.Equal("true", bString);
    }

    [Fact]
    public void ClassIsSealed()
    {
        Assert.True(typeof(BooleanValueNode).IsSealed);
    }

    [Fact]
    public void BooleanValue_WithNewValue_NewValueIsSet()
    {
        // arrange
        var booleanValueNode = new BooleanValueNode(false);

        // act
        booleanValueNode = booleanValueNode.WithValue(true);

        // assert
        Assert.True(booleanValueNode.Value);
    }
}
