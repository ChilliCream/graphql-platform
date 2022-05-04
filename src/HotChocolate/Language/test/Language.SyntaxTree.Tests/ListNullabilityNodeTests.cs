using Xunit;

namespace HotChocolate.Language.SyntaxTree;

public class ListNullabilityNodeTests
{
     [Fact]
    public void Equals_With_Same_Location()
    {
        var a = new ListNullabilityNode(
            TestLocations.Location1,
            new RequiredModifierNode(
                null,
                new ListNullabilityNode(null, null)));
        var b = new ListNullabilityNode(
            TestLocations.Location1,
            new RequiredModifierNode(
                null,
                new ListNullabilityNode(null, null)));
        var c = new ListNullabilityNode(
            TestLocations.Location1,
            null);

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
        var a = new ListNullabilityNode(
            TestLocations.Location1,
            new RequiredModifierNode(
                null,
                new ListNullabilityNode(null, null)));
        var b = new ListNullabilityNode(
            TestLocations.Location2,
            new RequiredModifierNode(
                null,
                new ListNullabilityNode(null, null)));
        var c = new ListNullabilityNode(
            TestLocations.Location1,
            null);

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
        var a = new ListNullabilityNode(
            TestLocations.Location1,
            new RequiredModifierNode(
                null,
                new ListNullabilityNode(null, null)));
        var b = new ListNullabilityNode(
            TestLocations.Location2,
            new RequiredModifierNode(
                null,
                new ListNullabilityNode(null, null)));
        var c = new ListNullabilityNode(
            TestLocations.Location1,
            null);
        var d = new ListNullabilityNode(
            TestLocations.Location2,
            null);

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
