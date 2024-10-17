using Xunit;

namespace HotChocolate.Language.SyntaxTree;

public class SemanticNonNullTypeNodeTests
{
    [Fact]
    public void Create_With_Type()
    {
        // arrange
        var namedType = new NamedTypeNode("abc");

        // act
        var type = new SemanticNonNullTypeNode(namedType);

        // assert
        Assert.Equal(namedType, type.Type);
    }

    [Fact]
    public void Create_With_Type_Where_Type_Is_Null()
    {
        // arrange
        // act
        Action action = () => new SemanticNonNullTypeNode(null!);

        // assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public void Create_With_Location_And_Type()
    {
        // arrange
        var location = new Location(1, 1, 1, 1);
        var namedType = new NamedTypeNode("abc");

        // act
        var type = new SemanticNonNullTypeNode(location, namedType);

        // assert
        Assert.Equal(location, type.Location);
        Assert.Equal(namedType, type.Type);
    }

    [Fact]
    public void Create_With_Location_And_Type_Where_Location_Is_Null()
    {
        // arrange
        var namedType = new NamedTypeNode("abc");

        // act
        var type = new SemanticNonNullTypeNode(null, namedType);

        // assert
        Assert.Null(type.Location);
        Assert.Equal(namedType, type.Type);
    }

    [Fact]
    public void Create_With_Location_And_Type_Where_Type_Is_Null()
    {
        // arrange
        var location = new Location(1, 1, 1, 1);

        // act
        Action action = () => new SemanticNonNullTypeNode(location, null!);

        // assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public void Equals_With_Same_Location()
    {
        // arrange
        var a = new SemanticNonNullTypeNode(TestLocations.Location1, new NamedTypeNode("aa"));
        var b = new SemanticNonNullTypeNode(TestLocations.Location1, new NamedTypeNode("aa"));
        var c = new SemanticNonNullTypeNode(TestLocations.Location1, new NamedTypeNode("ab"));

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
        var a = new SemanticNonNullTypeNode(TestLocations.Location1, new NamedTypeNode("aa"));
        var b = new SemanticNonNullTypeNode(TestLocations.Location2, new NamedTypeNode("aa"));
        var c = new SemanticNonNullTypeNode(TestLocations.Location1, new NamedTypeNode("ab"));

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
        var a = new SemanticNonNullTypeNode(TestLocations.Location1, new NamedTypeNode("aa"));
        var b = new SemanticNonNullTypeNode(TestLocations.Location2, new NamedTypeNode("aa"));
        var c = new SemanticNonNullTypeNode(TestLocations.Location1, new NamedTypeNode("ab"));
        var d = new SemanticNonNullTypeNode(TestLocations.Location2, new NamedTypeNode("ab"));

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
