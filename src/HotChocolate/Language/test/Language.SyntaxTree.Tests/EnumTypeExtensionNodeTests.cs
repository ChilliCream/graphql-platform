using Xunit;

namespace HotChocolate.Language.SyntaxTree;

public class EnumTypeExtensionNodeTests
{
    [Fact]
    public void EnumTypeExtensionWithLocation()
    {
        // arrange
        var location = new Location(0, 0, 0, 0);
        var name = new NameNode("foo");
        var directives = new List<DirectiveNode>();
        var values = new List<EnumValueDefinitionNode>();

        // act
        var type = new EnumTypeExtensionNode(
            location,
            name,
            directives,
            values);

        // assert
        Assert.Equal(SyntaxKind.EnumTypeExtension, type.Kind);
        Assert.Equal(location, type.Location);
        Assert.Equal(name, type.Name);
        Assert.Equal(directives, type.Directives);
        Assert.Equal(values, type.Values);
    }

    [Fact]
    public void EnumTypeExtensionWithoutLocation()
    {
        // arrange
        var name = new NameNode("foo");
        var directives = new List<DirectiveNode>();
        var values = new List<EnumValueDefinitionNode>();

        // act
        var type = new EnumTypeExtensionNode(
            null,
            name,
            directives,
            values);

        // assert
        Assert.Equal(SyntaxKind.EnumTypeExtension, type.Kind);
        Assert.Null(type.Location);
        Assert.Equal(name, type.Name);
        Assert.Equal(directives, type.Directives);
        Assert.Equal(values, type.Values);
    }

    [Fact]
    public void EnumTypeExtensionWithoutName_ArgumentNullException()
    {
        // arrange
        var location = new Location(0, 0, 0, 0);
        var directives = new List<DirectiveNode>();
        var values = new List<EnumValueDefinitionNode>();

        // act
        EnumTypeExtensionNode Action()
            => new(location, null!, directives, values);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public void EnumTypeExtensionWithoutDirectives_ArgumentNullException()
    {
        // arrange
        var location = new Location(0, 0, 0, 0);
        var name = new NameNode("foo");
        var values = new List<EnumValueDefinitionNode>();

        // act
        EnumTypeExtensionNode Action()
            => new(location, name, null!, values);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public void EnumTypeExtensionWithoutValues_ArgumentNullException()
    {
        // arrange
        var location = new Location(0, 0, 0, 0);
        var name = new NameNode("foo");
        var directives = new List<DirectiveNode>();

        // act
        Action a = () => new EnumTypeExtensionNode(
            location,
            name,
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
        var directives = new List<DirectiveNode>();
        var values = new List<EnumValueDefinitionNode>();

        var type = new EnumTypeExtensionNode(
           location,
           name,
           directives,
           values);

        // act
        type = type.WithName(new NameNode("baz"));

        // assert
        Assert.Equal("baz", type.Name.Value);
    }

    [Fact]
    public void WithDirectives()
    {
        // arrange
        var location = new Location(0, 0, 0, 0);
        var name = new NameNode("foo");
        var directives = new List<DirectiveNode>();
        var values = new List<EnumValueDefinitionNode>();

        var type = new EnumTypeExtensionNode(
           location,
           name,
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
        var directives = new List<DirectiveNode>();
        var values = new List<EnumValueDefinitionNode>();

        var type = new EnumTypeExtensionNode(
           location,
           name,
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
        var directives = new List<DirectiveNode>();
        var values = new List<EnumValueDefinitionNode>();

        var type = new EnumTypeExtensionNode(
           null,
           name,
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

        var a = new EnumTypeExtensionNode(
            TestLocations.Location1,
            new("aa"),
            Array.Empty<DirectiveNode>(),
            values);
        var b = new EnumTypeExtensionNode(
            TestLocations.Location1,
            new("aa"),
            Array.Empty<DirectiveNode>(),
            values);
        var c = new EnumTypeExtensionNode(
            TestLocations.Location1,
            new("ab"),
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

        var a = new EnumTypeExtensionNode(
            TestLocations.Location1,
            new("aa"),
            Array.Empty<DirectiveNode>(),
            values);
        var b = new EnumTypeExtensionNode(
            TestLocations.Location2,
            new("aa"),
            Array.Empty<DirectiveNode>(),
            values);
        var c = new EnumTypeExtensionNode(
            TestLocations.Location1,
            new("ab"),
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

        var a = new EnumTypeExtensionNode(
            TestLocations.Location1,
            new("aa"),
            Array.Empty<DirectiveNode>(),
            values);
        var b = new EnumTypeExtensionNode(
            TestLocations.Location2,
            new("aa"),
            Array.Empty<DirectiveNode>(),
            values);
        var c = new EnumTypeExtensionNode(
            TestLocations.Location1,
            new("ab"),
            Array.Empty<DirectiveNode>(),
            values);
        var d = new EnumTypeExtensionNode(
            TestLocations.Location2,
            new("ab"),
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
