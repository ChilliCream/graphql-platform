using Xunit;

namespace HotChocolate.Language.SyntaxTree;

public class NamedTypeNodeTests
{
    [Fact]
    public void Equals_With_Same_Location()
    {
        var a = new NamedTypeNode(
            TestLocations.Location1,
            new("aa"));
        var b = new NamedTypeNode(
            TestLocations.Location1,
            new("aa"));
        var c = new NamedTypeNode(
            TestLocations.Location1,
            new("bb"));

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
        var a = new NamedTypeNode(
            TestLocations.Location1,
            new("aa"));
        var b = new NamedTypeNode(
            TestLocations.Location2,
            new("aa"));
        var c = new NamedTypeNode(
            TestLocations.Location1,
            new("bb"));

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
        var a = new NamedTypeNode(
            TestLocations.Location1,
            new("aa"));
        var b = new NamedTypeNode(
            TestLocations.Location2,
            new("aa"));
        var c = new NamedTypeNode(
            TestLocations.Location1,
            new("bb"));
        var d = new NamedTypeNode(
            TestLocations.Location2,
            new("bb"));

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
