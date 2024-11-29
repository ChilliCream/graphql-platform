using Xunit;

namespace HotChocolate.Language.SyntaxTree;

public class EnumTypeDefinitionNodeTests
{
    [Fact]
    public void EnumTypeDefinitionWithLocation()
    {
        // arrange
        var location = new Location(0, 0, 0, 0);
        var name = new NameNode("foo");
        var description = new StringValueNode("bar");
        var directives = new List<DirectiveNode>();
        var values = new List<EnumValueDefinitionNode>();

        // act
        var type = new EnumTypeDefinitionNode(
            location,
            name,
            description,
            directives,
            values);

        // assert
        Assert.Equal(SyntaxKind.EnumTypeDefinition, type.Kind);
        Assert.Equal(location, type.Location);
        Assert.Equal(name, type.Name);
        Assert.Equal(description, type.Description);
        Assert.Equal(directives, type.Directives);
        Assert.Equal(values, type.Values);
    }

    [Fact]
    public void EnumTypeDefinitionWithoutLocation()
    {
        // arrange
        var name = new NameNode("foo");
        var description = new StringValueNode("bar");
        var directives = new List<DirectiveNode>();
        var values = new List<EnumValueDefinitionNode>();

        // act
        var type = new EnumTypeDefinitionNode(
            null,
            name,
            description,
            directives,
            values);

        // assert
        Assert.Equal(SyntaxKind.EnumTypeDefinition, type.Kind);
        Assert.Null(type.Location);
        Assert.Equal(name, type.Name);
        Assert.Equal(description, type.Description);
        Assert.Equal(directives, type.Directives);
        Assert.Equal(values, type.Values);
    }

    [Fact]
    public void EnumTypeDefinitionWithoutName_ArgumentNullException()
    {
        // arrange
        var location = new Location(0, 0, 0, 0);
        var description = new StringValueNode("bar");
        var directives = new List<DirectiveNode>();
        var values = new List<EnumValueDefinitionNode>();

        // act
        EnumTypeDefinitionNode Action()
            => new(location, null!, description, directives, values);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public void EnumTypeDefinitionWithoutDirectives_ArgumentNullException()
    {
        // arrange
        var location = new Location(0, 0, 0, 0);
        var name = new NameNode("foo");
        var description = new StringValueNode("bar");
        var values = new List<EnumValueDefinitionNode>();

        // act
        EnumTypeDefinitionNode Action()
            => new(location, name, description, null!, values);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public void EnumTypeDefinitionWithoutValues_ArgumentNullException()
    {
        // arrange
        var location = new Location(0, 0, 0, 0);
        var name = new NameNode("foo");
        var description = new StringValueNode("bar");
        var directives = new List<DirectiveNode>();

        // act
        Action a = () => new EnumTypeDefinitionNode(
             location,
             name,
             description,
             directives,
             null!);

        // assert
        Assert.Throws<ArgumentNullException>(a);
    }

    [Fact]
    public void WithName()
    {
        // arrange
        var location = new Location(0, 0, 0, 0);
        var name = new NameNode("foo");
        var description = new StringValueNode("bar");
        var directives = new List<DirectiveNode>();
        var values = new List<EnumValueDefinitionNode>();

        var type = new EnumTypeDefinitionNode(
           location,
           name,
           description,
           directives,
           values);

        // act
        type = type.WithName(new NameNode("baz"));

        // assert
        Assert.Equal("baz", type.Name.Value);
    }

    [Fact]
    public void WithDescription()
    {
        // arrange
        var location = new Location(0, 0, 0, 0);
        var name = new NameNode("foo");
        var description = new StringValueNode("bar");
        var directives = new List<DirectiveNode>();
        var values = new List<EnumValueDefinitionNode>();

        var type = new EnumTypeDefinitionNode(
           location,
           name,
           description,
           directives,
           values);

        // act
        type = type.WithDescription(new StringValueNode("baz"));

        // assert
        Assert.Equal("baz", type.Description!.Value);
    }

    [Fact]
    public void WithDirectives()
    {
        // arrange
        var location = new Location(0, 0, 0, 0);
        var name = new NameNode("foo");
        var description = new StringValueNode("bar");
        var directives = new List<DirectiveNode>();
        var values = new List<EnumValueDefinitionNode>();

        var type = new EnumTypeDefinitionNode(
           location,
           name,
           description,
           new List<DirectiveNode>(),
           values);

        // act
        type = type.WithDirectives(directives);

        // assert
        Assert.Equal(directives, type.Directives);
    }

    [Fact]
    public void WithValues()
    {
        // arrange
        var location = new Location(0, 0, 0, 0);
        var name = new NameNode("foo");
        var description = new StringValueNode("bar");
        var directives = new List<DirectiveNode>();
        var values = new List<EnumValueDefinitionNode>();

        var type = new EnumTypeDefinitionNode(
           location,
           name,
           description,
           directives,
           new List<EnumValueDefinitionNode>());

        // act
        type = type.WithValues(values);

        // assert
        Assert.Equal(values, type.Values);
    }

    [Fact]
    public void WithLocation()
    {
        // arrange
        var location = new Location(0, 0, 0, 0);
        var name = new NameNode("foo");
        var description = new StringValueNode("bar");
        var directives = new List<DirectiveNode>();
        var values = new List<EnumValueDefinitionNode>();

        var type = new EnumTypeDefinitionNode(
           null,
           name,
           description,
           directives,
           values);

        // act
        type = type.WithLocation(location);

        // assert
        Assert.Equal(location, type.Location);
    }

    [Fact]
    public void Equals_With_Same_Location()
    {
        // arrange
        var values = new List<EnumValueDefinitionNode>
        {
            new EnumValueDefinitionNode(null, new("DEF"), null, Array.Empty<DirectiveNode>()),
        };

        var a = new EnumTypeDefinitionNode(
            TestLocations.Location1,
            new("aa"),
            null,
            Array.Empty<DirectiveNode>(),
            values);
        var b = new EnumTypeDefinitionNode(
            TestLocations.Location1,
            new("aa"),
            null,
            Array.Empty<DirectiveNode>(),
            values);
        var c = new EnumTypeDefinitionNode(
            TestLocations.Location1,
            new("ab"),
            null,
            Array.Empty<DirectiveNode>(),
            values);

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
        var values = new List<EnumValueDefinitionNode>
        {
            new EnumValueDefinitionNode(null, new("DEF"), null, Array.Empty<DirectiveNode>()),
        };

        var a = new EnumTypeDefinitionNode(
            TestLocations.Location1,
            new("aa"),
            null,
            Array.Empty<DirectiveNode>(),
            values);
        var b = new EnumTypeDefinitionNode(
            TestLocations.Location2,
            new("aa"),
            null,
            Array.Empty<DirectiveNode>(),
            values);
        var c = new EnumTypeDefinitionNode(
            TestLocations.Location1,
            new("ab"),
            null,
            Array.Empty<DirectiveNode>(),
            values);

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
        var values = new List<EnumValueDefinitionNode>
        {
            new EnumValueDefinitionNode(null, new("DEF"), null, Array.Empty<DirectiveNode>()),
        };

        var a = new EnumTypeDefinitionNode(
            TestLocations.Location1,
            new("aa"),
            null,
            Array.Empty<DirectiveNode>(),
            values);
        var b = new EnumTypeDefinitionNode(
            TestLocations.Location2,
            new("aa"),
            null,
            Array.Empty<DirectiveNode>(),
            values);
        var c = new EnumTypeDefinitionNode(
            TestLocations.Location1,
            new("ab"),
            null,
            Array.Empty<DirectiveNode>(),
            values);
        var d = new EnumTypeDefinitionNode(
            TestLocations.Location2,
            new("ab"),
            null,
            Array.Empty<DirectiveNode>(),
            values);

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
