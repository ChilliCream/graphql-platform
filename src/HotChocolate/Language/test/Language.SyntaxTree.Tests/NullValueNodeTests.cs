using Xunit;

namespace HotChocolate.Language.SyntaxTree;

public class NullValueNodeTests
{
    [Fact]
    public void Equals_With_Same_Location()
    {
        var a = new NullValueNode(
            TestLocations.Location1);
        var b = new NullValueNode(
            TestLocations.Location1);

        // act
        var abResult = a.Equals(b);
        var aaResult = a.Equals(a);
        var aNullResult = a.Equals(default);

        // assert
        Assert.True(abResult);
        Assert.True(aaResult);
        Assert.False(aNullResult);
    }

    [Fact]
    public void Equals_With_Different_Location()
    {
        // arrange
        var a = new NullValueNode(
            TestLocations.Location1);
        var b = new NullValueNode(
            TestLocations.Location2);

        // act
        var abResult = a.Equals(b);
        var aaResult = a.Equals(a);
        var aNullResult = a.Equals(default);

        // assert
        Assert.True(abResult);
        Assert.True(aaResult);
        Assert.False(aNullResult);
    }

    [Fact]
    public void GetHashCode_With_Location()
    {
        // arrange
        var a = new NullValueNode(
            TestLocations.Location1);
        var b = new NullValueNode(
            TestLocations.Location2);

        // act
        var aHash = a.GetHashCode();
        var bHash = b.GetHashCode();

        // assert
        Assert.Equal(aHash, bHash);
    }
}
