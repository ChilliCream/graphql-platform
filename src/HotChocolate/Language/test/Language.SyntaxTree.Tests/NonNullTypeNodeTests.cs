using Xunit;

namespace HotChocolate.Language.SyntaxTree;

public class NonNullTypeNodeTests
{
    [Fact]
    public void Create_With_Type()
    {
        // arrange
        var namedType = new NamedTypeNode("abc");

        // act
        var type = new NonNullTypeNode(namedType);

        // assert
        Assert.Equal(namedType, type.Type);
    }

    [Fact]
    public void Create_With_Type_Where_Type_Is_Null()
    {
        // arrange
        // act
        Action action = () => new NonNullTypeNode(null!);

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
        var type = new NonNullTypeNode(location, namedType);

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
        var type = new NonNullTypeNode(null, namedType);

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
        Action action = () => new NonNullTypeNode(location, null!);

        // assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public void WithLocation()
    {
        // arrange
        var initialLocation = new Location(1, 1, 1, 1);
        var namedType = new NamedTypeNode("abc");
        var type = new NonNullTypeNode(initialLocation, namedType);

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
        var type = new NonNullTypeNode(initialLocation, namedType);

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
        var type = new NonNullTypeNode(location, initialType);

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
        var type = new NonNullTypeNode(location, initialType);

        // act
        Action action = () => type.WithType(null!);

        // assert
        Assert.Throws<ArgumentNullException>(action);
    }
}
