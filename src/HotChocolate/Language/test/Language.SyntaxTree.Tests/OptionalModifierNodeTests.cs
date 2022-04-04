using Xunit;

namespace Language.SyntaxTree.Tests;

public class OptionalModifierNodeTests
{
    [Fact]
    public void Equals_With_Same_Location()
    {
        // arrange
        var a = new OptionalModifierNode(new Location(1, 1, 1, 1), new("aa"), new IntValueNode(123));
        var b = new ArgumentNode(new Location(1, 1, 1, 1), new("aa"), new IntValueNode(123));
        var c = new ArgumentNode(new Location(1, 1, 1, 1), new("aa"), new IntValueNode(567));

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
        var a = new ArgumentNode(new Location(1, 1, 1, 1), new("aa"), new IntValueNode(123));
        var b = new ArgumentNode(new Location(2, 2, 2, 2), new("aa"), new IntValueNode(123));
        var c = new ArgumentNode(new Location(3, 3, 3, 3), new("aa"), new IntValueNode(567));

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
        var a = new ArgumentNode(new Location(1, 1, 1, 1), new("aa"), new IntValueNode(123));
        var b = new ArgumentNode(new Location(2, 2, 2, 2), new("aa"), new IntValueNode(123));
        var c = new ArgumentNode(new Location(1, 1, 1, 1), new("aa"), new IntValueNode(567));
        var d = new ArgumentNode(new Location(2, 2, 2, 2), new("aa"), new IntValueNode(567));

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
