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
        bool result = a.Equals(b);

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
        var result = a.Equals(b);

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
        var abResult = a.Equals(b);
        var aaResult = a.Equals(a);
        var acResult = a.Equals(c);
        var aNullResult = a.Equals(default);

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
        var abResult = a.Equals(b);
        var aaResult = a.Equals(a);
        var acResult = a.Equals(c);
        var aNullResult = a.Equals(default);

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
        var aHash = a.GetHashCode();
        var bHash = b.GetHashCode();
        var cHash = c.GetHashCode();
        var dHash = d.GetHashCode();

        // assert
        Assert.Equal(aHash, bHash);
        Assert.NotEqual(aHash, cHash);
        Assert.Equal(cHash, dHash);
        Assert.NotEqual(aHash, dHash);
    }
}
