namespace HotChocolate.Language.SyntaxTree;

public class EnumValueNodeTests
{
    [Fact]
    public void Equals_With_Same_Location()
    {
        var a = new EnumValueNode(
            TestLocations.Location1,
            "AA");
        var b = new EnumValueNode(
            TestLocations.Location1,
            "AA");
        var c = new EnumValueNode(
            TestLocations.Location1,
            "AB");

        // act
        var abResult = SyntaxComparer.BySyntax.Equals(a, b);
        var aaResult = SyntaxComparer.BySyntax.Equals(a, a);
        var acResult = SyntaxComparer.BySyntax.Equals(a, c);
        var aNullResult = SyntaxComparer.BySyntax.Equals(a, null);

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
        var a = new EnumValueNode(
            TestLocations.Location1,
            "AA");
        var b = new EnumValueNode(
            TestLocations.Location2,
            "AA");
        var c = new EnumValueNode(
            TestLocations.Location1,
            "AB");

        // act
        var abResult = SyntaxComparer.BySyntax.Equals(a, b);
        var aaResult = SyntaxComparer.BySyntax.Equals(a, a);
        var acResult = SyntaxComparer.BySyntax.Equals(a, c);
        var aNullResult = SyntaxComparer.BySyntax.Equals(a, null);

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
        var a = new EnumValueNode(
            TestLocations.Location1,
            "AA");
        var b = new EnumValueNode(
            TestLocations.Location2,
            "AA");
        var c = new EnumValueNode(
            TestLocations.Location1,
            "AB");
        var d = new EnumValueNode(
            TestLocations.Location2,
            "AB");

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
