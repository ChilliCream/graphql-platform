using Xunit;

namespace HotChocolate.Language.SyntaxTree.Tests;

public class SchemaCoordinateNodeTests
{
    [Fact]
    public void Equals_With_Same_Location()
    {
        // arrange
        var a = new SchemaCoordinateNode(
            new Location(1, 1, 1, 1),
            false,
            new NameNode("aa"),
            new NameNode("aa"),
            new NameNode("aa"));
        var b = new SchemaCoordinateNode(
            new Location(1, 1, 1, 1),
            false,
            new NameNode("aa"),
            new NameNode("aa"),
            new NameNode("aa"));
        var c = new SchemaCoordinateNode(
            new Location(1, 1, 1, 1),
            false,
            new NameNode("bb"),
            new NameNode("bb"),
            new NameNode("bb"));

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
        var a = new SchemaCoordinateNode(
            new Location(1, 1, 1, 1),
            false,
            new NameNode("aa"),
            new NameNode("aa"),
            new NameNode("aa"));
        var b = new SchemaCoordinateNode(
            new Location(2, 2, 2, 2),
            false,
            new NameNode("aa"),
            new NameNode("aa"),
            new NameNode("aa"));
        var c = new SchemaCoordinateNode(
            new Location(3, 3, 3, 3),
            false,
            new NameNode("bb"),
            new NameNode("bb"),
            new NameNode("bb"));

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
        var a = new SchemaCoordinateNode(
            new Location(1, 1, 1, 1),
            false,
            new NameNode("aa"),
            new NameNode("aa"),
            new NameNode("aa"));
        var b = new SchemaCoordinateNode(
            new Location(2, 2, 2, 2),
            false,
            new NameNode("aa"),
            new NameNode("aa"),
            new NameNode("aa"));
        var c = new SchemaCoordinateNode(
            new Location(1, 1, 1, 1),
            false,
            new NameNode("bb"),
            new NameNode("bb"),
            new NameNode("bb"));
        var d = new SchemaCoordinateNode(
            new Location(2, 2, 2, 2),
            false,
            new NameNode("bb"),
            new NameNode("bb"),
            new NameNode("bb"));

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
