using Xunit;

namespace HotChocolate.Language.SyntaxTree;

public class RequiredModifierNodeTests
{
    private readonly ListNullabilityNode _element1 =
        new(new Location(1, 1, 1, 1), null);
    private readonly ListNullabilityNode _element2 =
        new(new Location(1, 1, 1, 1), new RequiredModifierNode(null, null));

    [Fact]
    public void Equals_With_Same_Location()
    {
        // arrange
        var a = new RequiredModifierNode(new Location(1, 1, 1, 1), _element1);
        var b = new RequiredModifierNode(new Location(1, 1, 1, 1), _element1);
        var c = new RequiredModifierNode(new Location(1, 1, 1, 1), _element2);

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
        var a = new RequiredModifierNode(new Location(1, 1, 1, 1), _element1);
        var b = new RequiredModifierNode(new Location(2, 2, 2, 2), _element1);
        var c = new RequiredModifierNode(new Location(3, 3, 3, 3), _element2);

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
        var a = new RequiredModifierNode(new Location(1, 1, 1, 1), _element1);
        var b = new RequiredModifierNode(new Location(2, 2, 2, 2), _element1);
        var c = new RequiredModifierNode(new Location(1, 1, 1, 1), _element2);
        var d = new RequiredModifierNode(new Location(2, 2, 2, 2), _element2);

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
