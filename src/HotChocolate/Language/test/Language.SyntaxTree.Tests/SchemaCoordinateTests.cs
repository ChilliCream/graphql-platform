using Xunit;

namespace HotChocolate.Language.SyntaxTree;

public class SchemaCoordinateTests
{
    [Fact]
    public void CreateSchemaCoordinateWithLocation()
    {
        // arrange
        var location = new Location(0, 0, 0, 0);
        var ofDirective = false;
        var name = new NameNode("Foo");
        var memberName = new NameNode("bar");
        var argumentName = new NameNode("baz");

        // act
        var coordinate =
            new SchemaCoordinateNode(location, ofDirective, name, memberName, argumentName);

        // assert
        Assert.Equal(SyntaxKind.SchemaCoordinate, coordinate.Kind);
        Assert.Equal(location, coordinate.Location);
        Assert.False(coordinate.OfDirective);
        Assert.Equal(name, coordinate.Name);
        Assert.Equal(memberName, coordinate.MemberName);
        Assert.Equal(argumentName, coordinate.ArgumentName);
        coordinate.ToString().MatchSnapshot();
    }

    [Fact]
    public void CreateSchemaCoordinateWithoutLocation()
    {
        // arrange
        var ofDirective = false;
        var name = new NameNode("Foo");
        var memberName = new NameNode("bar");
        var argumentName = new NameNode("baz");

        // act
        var coordinate =
            new SchemaCoordinateNode(null, ofDirective, name, memberName, argumentName);

        // assert
        Assert.Equal(SyntaxKind.SchemaCoordinate, coordinate.Kind);
        Assert.Null(coordinate.Location);
        Assert.False(coordinate.OfDirective);
        Assert.Equal(name, coordinate.Name);
        Assert.Equal(memberName, coordinate.MemberName);
        Assert.Equal(argumentName, coordinate.ArgumentName);
        coordinate.ToString().MatchSnapshot();
    }

    [Fact]
    public void CreateSchemaCoordinateWithoutArgumentName()
    {
        // arrange
        var location = new Location(0, 0, 0, 0);
        var ofDirective = false;
        var name = new NameNode("Foo");
        var memberName = new NameNode("bar");

        // act
        var coordinate =
            new SchemaCoordinateNode(location, ofDirective, name, memberName, null);

        // assert
        Assert.Equal(SyntaxKind.SchemaCoordinate, coordinate.Kind);
        Assert.Equal(location, coordinate.Location);
        Assert.False(coordinate.OfDirective);
        Assert.Equal(name, coordinate.Name);
        Assert.Equal(memberName, coordinate.MemberName);
        Assert.Null(coordinate.ArgumentName);
        coordinate.ToString().MatchSnapshot();
    }

    [Fact]
    public void CreateSchemaCoordinateWithoutArgumentAndMemberName()
    {
        // arrange
        var location = new Location(0, 0, 0, 0);
        var ofDirective = false;
        var name = new NameNode("Foo");

        // act
        var coordinate =
            new SchemaCoordinateNode(location, ofDirective, name, null, null);

        // assert
        Assert.Equal(SyntaxKind.SchemaCoordinate, coordinate.Kind);
        Assert.Equal(location, coordinate.Location);
        Assert.False(coordinate.OfDirective);
        Assert.Equal(name, coordinate.Name);
        Assert.Null(coordinate.MemberName);
        Assert.Null(coordinate.ArgumentName);
        coordinate.ToString().MatchSnapshot();
    }

    [Fact]
    public void CreateSchemaCoordinateOfDirectiveWithArgumentName()
    {
        // arrange
        var location = new Location(0, 0, 0, 0);
        var ofDirective = true;
        var name = new NameNode("Foo");
        var argumentName = new NameNode("baz");

        // act
        var coordinate =
            new SchemaCoordinateNode(location, ofDirective, name, null, argumentName);

        // assert
        Assert.Equal(SyntaxKind.SchemaCoordinate, coordinate.Kind);
        Assert.Equal(location, coordinate.Location);
        Assert.True(coordinate.OfDirective);
        Assert.Equal(name, coordinate.Name);
        Assert.Null(coordinate.MemberName);
        Assert.Equal(argumentName, coordinate.ArgumentName);
        coordinate.ToString().MatchSnapshot();
    }

    [Fact]
    public void CreateSchemaCoordinateOfDirectiveWithoutArgumentName()
    {
        // arrange
        var location = new Location(0, 0, 0, 0);
        var ofDirective = true;
        var name = new NameNode("Foo");

        // act
        var coordinate =
            new SchemaCoordinateNode(location, ofDirective, name, null, null);

        // assert
        Assert.Equal(SyntaxKind.SchemaCoordinate, coordinate.Kind);
        Assert.Equal(location, coordinate.Location);
        Assert.True(coordinate.OfDirective);
        Assert.Equal(name, coordinate.Name);
        Assert.Null(coordinate.MemberName);
        Assert.Null(coordinate.ArgumentName);
        coordinate.ToString().MatchSnapshot();
    }

    [Fact]
    public void CreateSchemaCoordinateOfDirectiveWithMemberName()
    {
        // arrange
        var location = new Location(0, 0, 0, 0);
        var ofDirective = true;
        var name = new NameNode("Foo");
        var memberName = new NameNode("Foo");

        // act
        var ex = Record.Exception(() =>
        {
            new SchemaCoordinateNode(location, ofDirective, name, memberName, null);
        });

        // assert
        Assert.IsType<ArgumentException>(ex);
    }

    [Fact]
    public void CreateSchemaCoordinateOfTypeWithArgumentNameButNoMemberName()
    {
        // arrange
        var location = new Location(0, 0, 0, 0);
        var ofDirective = false;
        var name = new NameNode("Foo");
        var argumentName = new NameNode("baz");

        // act
        var ex = Record.Exception(() =>
        {
            new SchemaCoordinateNode(location, ofDirective, name, null, argumentName);
        });

        // assert
        Assert.IsType<ArgumentException>(ex);
    }

    [Fact]
    public void CreateSchemaCoordinateWithoutName()
    {
        // arrange
        var location = new Location(0, 0, 0, 0);
        var ofDirective = false;

        // act
        var ex = Record.Exception(() =>
        {
            new SchemaCoordinateNode(location, ofDirective, null!, null, null);
        });

        // assert
        Assert.IsType<ArgumentNullException>(ex);
    }

    [Fact]
    public void SchemaCoordinate_With_Location()
    {
        // arrange
        var location = new Location(0, 0, 0, 0);
        var name = new NameNode("Foo");
        var node = new SchemaCoordinateNode(null, false, name, null, null);

        // act
        var rewrittenNode = node.WithLocation(location);

        // assert
        Assert.Equal(location, rewrittenNode.Location);
    }

    [Fact]
    public void SchemaCoordinate_With_Name()
    {
        // arrange
        var name = new NameNode("Foo");
        var newName = new NameNode("newName");
        var node = new SchemaCoordinateNode(null, false, name, null, null);

        // act
        var rewrittenNode = node.WithName(newName);

        // assert
        Assert.Equal(newName, rewrittenNode.Name);
    }

    [Fact]
    public void SchemaCoordinate_With_MemberName()
    {
        // arrange
        var name = new NameNode("Foo");
        var memberName = new NameNode("foo");
        var node = new SchemaCoordinateNode(null, false, name, null, null);

        // act
        var rewrittenNode = node.WithMemberName(memberName);

        // assert
        Assert.Equal(memberName, rewrittenNode.MemberName);
    }

    [Fact]
    public void SchemaCoordinate_With_ArgumentName()
    {
        // arrange
        var name = new NameNode("Foo");
        var memberName = new NameNode("foo");
        var argumentName = new NameNode("baz");
        var node = new SchemaCoordinateNode(null, false, name, memberName, null);

        // act
        var rewrittenNode = node.WithArgumentName(argumentName);

        // assert
        Assert.Equal(argumentName, rewrittenNode.ArgumentName);
    }

    [Fact]
    public void SchemaCoordinate_With_OfDirective()
    {
        // arrange
        var name = new NameNode("Foo");
        var node = new SchemaCoordinateNode(null, false, name, null, null);

        // act
        var rewrittenNode = node.WithOfDirective(true);

        // assert
        Assert.True(rewrittenNode.OfDirective);
    }
}
