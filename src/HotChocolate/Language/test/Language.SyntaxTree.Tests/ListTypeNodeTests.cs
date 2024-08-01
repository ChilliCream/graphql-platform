using Xunit;

namespace HotChocolate.Language.SyntaxTree;

public class ListTypeNodeTests
{
    [Fact]
    public void Create_With_Type()
    {
        // arrange
        var namedType = new NamedTypeNode("abc");

        // act
        var type = new ListTypeNode(namedType);

        // assert
        Assert.Equal(namedType, type.Type);
    }

    [Fact]
    public void Create_With_Type_Where_Type_Is_Null()
    {
        // arrange
        // act
        ListTypeNode Action() => new(null!);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public void Create_With_Location_And_Type()
    {
        // arrange
        var location = new Location(1, 1, 1, 1);
        var namedType = new NamedTypeNode("abc");

        // act
        var type = new ListTypeNode(location, namedType);

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
        var type = new ListTypeNode(null, namedType);

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
        ListTypeNode Action() => new(location, null!);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public void WithLocation()
    {
        // arrange
        var initialLocation = new Location(1, 1, 1, 1);
        var namedType = new NamedTypeNode("abc");
        var type = new ListTypeNode(initialLocation, namedType);

        // act
        var newLocation = new Location(2, 2, 2, 2);
        type = type.WithLocation(newLocation);

        // assert
        Assert.Equal(newLocation, type.Location);
        Assert.Equal(namedType, type.Type);
    }

    [Fact]
    public void WithLocation_Where_Location_Is_Null()
    {
        // arrange
        var initialLocation = new Location(1, 1, 1, 1);
        var namedType = new NamedTypeNode("abc");
        var type = new ListTypeNode(initialLocation, namedType);

        // act
        type = type.WithLocation(null);

        // assert
        Assert.Null(type.Location);
        Assert.Equal(namedType, type.Type);
    }

    [Fact]
    public void WithType()
    {
        // arrange
        var location = new Location(1, 1, 1, 1);
        var initialType = new NamedTypeNode("abc");
        var type = new ListTypeNode(location, initialType);

        // act
        var newType = new NamedTypeNode("def");
        type = type.WithType(newType);

        // assert
        Assert.Equal(location, type.Location);
        Assert.Equal(newType, type.Type);
    }

    [Fact]
    public void WithType_Where_Type_Is_Null()
    {
        // arrange
        var location = new Location(1, 1, 1, 1);
        var initialType = new NamedTypeNode("abc");
        var type = new ListTypeNode(location, initialType);

        // act
        void Action() => type.WithType(null!);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public void Equals_With_Same_Location()
    {
        var a = new ListTypeNode(
            TestLocations.Location1,
            new NamedTypeNode("Abc"));
        var b = new ListTypeNode(
            TestLocations.Location1,
            new NamedTypeNode("Abc"));
        var c = new ListTypeNode(
            TestLocations.Location1,
            new NamedTypeNode("Def"));

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
        var a = new ListTypeNode(
            TestLocations.Location1,
            new NamedTypeNode("Abc"));
        var b = new ListTypeNode(
            TestLocations.Location2,
            new NamedTypeNode("Abc"));
        var c = new ListTypeNode(
            TestLocations.Location1,
            new NamedTypeNode("Def"));

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
        var a = new ListTypeNode(
            TestLocations.Location1,
            new NamedTypeNode("Abc"));
        var b = new ListTypeNode(
            TestLocations.Location2,
            new NamedTypeNode("Abc"));
        var c = new ListTypeNode(
            TestLocations.Location1,
            new NamedTypeNode("Def"));
        var d = new ListTypeNode(
            TestLocations.Location2,
            new NamedTypeNode("Def"));

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
