using Xunit;

namespace HotChocolate.Language.SyntaxTree;

public class IntValueNodeTests
{
    [InlineData(1)]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(10000)]
    [InlineData(-10000)]
    [Theory]
    public void ValueNode_Equals(int value)
    {
        // arrange
        IValueNode a = new IntValueNode(value);
        IValueNode b = new IntValueNode(value);

        // act
        var result = SyntaxComparer.BySyntax.Equals(a, b);

        // assert
        Assert.True(result);
    }

    [InlineData(1, 2)]
    [InlineData(-1, -2)]
    [InlineData(0, 1)]
    [InlineData(10000, -5)]
    [InlineData(-10000, 45)]
    [Theory]
    public void ValueNode_NotEquals(int aValue, int bValue)
    {
        // arrange
        IValueNode a = new IntValueNode(aValue);
        IValueNode b = new IntValueNode(bValue);

        // act
        var result = SyntaxComparer.BySyntax.Equals(a, b);

        // assert
        Assert.False(result);
    }

    [Fact]
    public void Equals_With_Same_Location()
    {
        var a = new IntValueNode(
            TestLocations.Location1,
            1);
        var b = new IntValueNode(
            TestLocations.Location1,
            1);
        var c = new IntValueNode(
            TestLocations.Location1,
            2);

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
        var a = new IntValueNode(
            TestLocations.Location1,
            1);
        var b = new IntValueNode(
            TestLocations.Location2,
            1);
        var c = new IntValueNode(
            TestLocations.Location1,
            2);

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
        var a = new IntValueNode(
            TestLocations.Location1,
            1);
        var b = new IntValueNode(
            TestLocations.Location2,
            1);
        var c = new IntValueNode(
            TestLocations.Location1,
            2);
        var d = new IntValueNode(
            TestLocations.Location2,
            2);

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
