using Xunit;

namespace HotChocolate.Language.SyntaxTree;

public class DirectiveDefinitionNodeTests
{
    [InlineData(true)]
    [InlineData(false)]
    [Theory]
    public void CreateDirectiveDefinitionWithLocation(bool isRepeatable)
    {
        // arrange
        var location = new Location(0, 0, 0, 0);
        var name = new NameNode("foo");
        var description = new StringValueNode("bar");
        var arguments = new List<InputValueDefinitionNode>();
        var locations = new List<NameNode>();

        // act
        var directiveDefinition = new DirectiveDefinitionNode(
            location, name, description, isRepeatable,
            arguments, locations);

        // assert
        Assert.Equal(SyntaxKind.DirectiveDefinition, directiveDefinition.Kind);
        Assert.Equal(location, directiveDefinition.Location);
        Assert.Equal(name, directiveDefinition.Name);
        Assert.Equal(description, directiveDefinition.Description);
        Assert.Equal(isRepeatable, directiveDefinition.IsRepeatable);
        Assert.Equal(arguments, directiveDefinition.Arguments);
        Assert.Equal(locations, directiveDefinition.Locations);
    }

    [Fact]
    public void CreateDirectiveDefinition()
    {
        // arrange
        var name = new NameNode("foo");
        var description = new StringValueNode("bar");
        var arguments = new List<InputValueDefinitionNode>();
        var locations = new List<NameNode>();

        // act
        var directiveDefinition = new DirectiveDefinitionNode(
            null, name, description, true,
            arguments, locations);

        // assert
        Assert.Equal(SyntaxKind.DirectiveDefinition,
            directiveDefinition.Kind);
        Assert.Null(directiveDefinition.Location);
        Assert.Equal(name, directiveDefinition.Name);
        Assert.Equal(description, directiveDefinition.Description);
        Assert.Equal(arguments, directiveDefinition.Arguments);
        Assert.Equal(locations, directiveDefinition.Locations);
        Assert.True(directiveDefinition.IsRepeatable);
    }

    [Fact]
    public void WithName()
    {
        // arrange
        var name = new NameNode("foo");
        var description = new StringValueNode("bar");
        var arguments = new List<InputValueDefinitionNode>();
        var locations = new List<NameNode> { new(DirectiveLocation.Field.ToString()), };

        var directiveDefinition = new DirectiveDefinitionNode(
            null, name, description, true,
            arguments, locations);

        // act
        directiveDefinition = directiveDefinition
            .WithName(new NameNode("bar"));

        // assert
        directiveDefinition.MatchSnapshot();
    }

    [Fact]
    public void WithDescription()
    {
        // arrange
        var name = new NameNode("foo");
        var description = new StringValueNode("bar");
        var arguments = new List<InputValueDefinitionNode>();
        var locations = new List<NameNode> { new(DirectiveLocation.Field.ToString()), };

        var directiveDefinition = new DirectiveDefinitionNode(
            null, name, description, true,
            arguments, locations);

        // act
        directiveDefinition = directiveDefinition
            .WithDescription(new StringValueNode("qux"));

        // assert
        directiveDefinition.MatchSnapshot();
    }

    [Fact]
    public void WithArguments()
    {
        // arrange
        var name = new NameNode("foo");
        var description = new StringValueNode("bar");
        var arguments = new List<InputValueDefinitionNode>();
        var locations = new List<NameNode> { new(DirectiveLocation.Field.ToString()), };

        var directiveDefinition = new DirectiveDefinitionNode(
            null, name, description, true,
            arguments, locations);

        // act
        directiveDefinition = directiveDefinition
            .WithArguments(new List<InputValueDefinitionNode>
            {
                    new InputValueDefinitionNode
                    (
                        null,
                        new NameNode("arg"),
                        null,
                        new NamedTypeNode(new NameNode("type")),
                        NullValueNode.Default,
                        Array.Empty<DirectiveNode>()
                    ),
            });

        // assert
        directiveDefinition.MatchSnapshot();
    }

    [Fact]
    public void WithLocations()
    {
        // arrange
        var name = new NameNode("foo");
        var description = new StringValueNode("bar");
        var arguments = new List<InputValueDefinitionNode>();
        var locations = new List<NameNode>();

        var directiveDefinition = new DirectiveDefinitionNode(
            null, name, description, true,
            arguments, locations);

        // act
        directiveDefinition = directiveDefinition
            .WithLocations(new List<NameNode> { new NameNode("BAR"), });

        // assert
        directiveDefinition.MatchSnapshot();
    }

    [Fact]
    public void WithLocation()
    {
        // arrange
        var name = new NameNode("foo");
        var description = new StringValueNode("bar");
        var arguments = new List<InputValueDefinitionNode>();
        var locations = new List<NameNode> { new(DirectiveLocation.Field.ToString()), };

        var directiveDefinition = new DirectiveDefinitionNode(
            null, name, description, true,
            arguments, locations);

        // act
        directiveDefinition = directiveDefinition
            .WithLocation(AstTestHelper.CreateDummyLocation());

        // assert
        directiveDefinition.MatchSnapshot();
    }

    [Fact]
    public void AsUnique()
    {
        // arrange
        var name = new NameNode("foo");
        var description = new StringValueNode("bar");
        var arguments = new List<InputValueDefinitionNode>();
        var locations = new List<NameNode> { new(DirectiveLocation.Field.ToString()), };

        var directiveDefinition = new DirectiveDefinitionNode(
            null, name, description, true,
            arguments, locations);

        // act
        directiveDefinition = directiveDefinition.AsRepeatable(false);

        // assert
        directiveDefinition.MatchSnapshot();
    }

    [Fact]
    public void AsRepeatable()
    {
        // arrange
        var name = new NameNode("foo");
        var description = new StringValueNode("bar");
        var arguments = new List<InputValueDefinitionNode>();
        var locations = new List<NameNode> { new(DirectiveLocation.Field.ToString()), };

        var directiveDefinition = new DirectiveDefinitionNode(
            null, name, description, false,
            arguments, locations);

        // act
        directiveDefinition = directiveDefinition.AsRepeatable();

        // assert
        directiveDefinition.MatchSnapshot();
    }

    [Fact]
    public void DirectiveDefinition_ToString()
    {
        // arrange
        var name = new NameNode("foo");
        var description = new StringValueNode("bar");
        var arguments = new List<InputValueDefinitionNode>();
        var locations = new List<NameNode> { new(DirectiveLocation.Field.ToString()), };

        // act
        var directiveDefinition = new DirectiveDefinitionNode(
            null, name, description, true,
            arguments, locations);

        // assert
        directiveDefinition.ToString().MatchSnapshot();
    }

    [Fact]
    public void DirectiveDefinition_WithArgument_ToString()
    {
        // arrange
        var name = new NameNode("foo");
        var description = new StringValueNode("bar");
        var arguments = new List<InputValueDefinitionNode>
        {
            new(null,
                new NameNode("abc"),
                new StringValueNode("def"),
                new NamedTypeNode("efg"),
                null,
                Array.Empty<DirectiveNode>()),
        };
        var locations = new List<NameNode>
        {
            new(DirectiveLocation.Field.ToString()),
        };

        // act
        var directiveDefinition = new DirectiveDefinitionNode(
            null, name, description, true,
            arguments, locations);

        // assert
        directiveDefinition.ToString().MatchSnapshot();
    }

    [Fact]
    public void Equals_With_Same_Location()
    {
        // arrange
        var arguments = new List<InputValueDefinitionNode>
        {
            new(null,
                new NameNode("abc"),
                new StringValueNode("def"),
                new NamedTypeNode("efg"),
                null,
                Array.Empty<DirectiveNode>()),
        };

        var locations = new List<NameNode>
        {
            new(DirectiveLocation.Field.ToString()),
        };

        var a = new DirectiveDefinitionNode(
            TestLocations.Location1,
            new("aa"),
            null,
            true,
            arguments,
            locations);
        var b = new DirectiveDefinitionNode(
            TestLocations.Location1,
            new("aa"),
            null,
            true,
            arguments,
            locations);
        var c = new DirectiveDefinitionNode(
            TestLocations.Location1,
            new("bb"),
            null,
            true,
            arguments,
            locations);

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
        var arguments = new List<InputValueDefinitionNode>
        {
            new(null,
                new NameNode("abc"),
                new StringValueNode("def"),
                new NamedTypeNode("efg"),
                null,
                Array.Empty<DirectiveNode>()),
        };

        var locations = new List<NameNode>
        {
            new(DirectiveLocation.Field.ToString()),
        };

        var a = new DirectiveDefinitionNode(
            TestLocations.Location1,
            new("aa"),
            null,
            true,
            arguments,
            locations);
        var b = new DirectiveDefinitionNode(
            TestLocations.Location2,
            new("aa"),
            null,
            true,
            arguments,
            locations);
        var c = new DirectiveDefinitionNode(
            TestLocations.Location1,
            new("bb"),
            null,
            true,
            arguments,
            locations);

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
        var arguments = new List<InputValueDefinitionNode>
        {
            new(null,
                new NameNode("abc"),
                new StringValueNode("def"),
                new NamedTypeNode("efg"),
                null,
                Array.Empty<DirectiveNode>()),
        };

        var locations = new List<NameNode>
        {
            new(DirectiveLocation.Field.ToString()),
        };

        var a = new DirectiveDefinitionNode(
            TestLocations.Location1,
            new("aa"),
            null,
            true,
            arguments,
            locations);
        var b = new DirectiveDefinitionNode(
            TestLocations.Location2,
            new("aa"),
            null,
            true,
            arguments,
            locations);
        var c = new DirectiveDefinitionNode(
            TestLocations.Location1,
            new("bb"),
            null,
            true,
            arguments,
            locations);
        var d = new DirectiveDefinitionNode(
            TestLocations.Location2,
            new("bb"),
            null,
            true,
            arguments,
            locations);

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
