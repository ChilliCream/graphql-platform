using Xunit;

namespace HotChocolate.Language.SyntaxTree;

public class VariableDefinitionNodeTests
{
    [Fact]
    public void Equals_With_Same_Location()
    {
        // arrange
        var a = new VariableDefinitionNode(
            new Location(1, 1, 1, 1),
            new VariableNode("aa"),
            new NamedTypeNode("aa"),
            default,
            new List<DirectiveNode>(0));
        var b = new VariableDefinitionNode(
            new Location(1, 1, 1, 1),
            new VariableNode("aa"),
            new NamedTypeNode("aa"),
            default,
            new List<DirectiveNode>(0));
        var c = new VariableDefinitionNode(
            new Location(1, 1, 1, 1),
            new VariableNode("aa"),
            new NamedTypeNode("bb"),
            default,
            new List<DirectiveNode>(0));

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
        var a = new VariableDefinitionNode(
            new Location(1, 1, 1, 1),
            new VariableNode("aa"),
            new NamedTypeNode("aa"),
            default,
            new List<DirectiveNode>(0));
        var b = new VariableDefinitionNode(
            new Location(2, 2, 2, 2),
            new VariableNode("aa"),
            new NamedTypeNode("aa"),
            default,
            new List<DirectiveNode>(0));
        var c = new VariableDefinitionNode(
            new Location(3, 3, 3, 3),
            new VariableNode("aa"),
            new NamedTypeNode("bb"),
            default,
            new List<DirectiveNode>(0));

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
        var a = new VariableDefinitionNode(
            new Location(1, 1, 1, 1),
            new VariableNode("aa"),
            new NamedTypeNode("aa"),
            default,
            new List<DirectiveNode>(0));
        var b = new VariableDefinitionNode(
            new Location(2, 2, 2, 2),
            new VariableNode("aa"),
            new NamedTypeNode("aa"),
            default,
            new List<DirectiveNode>(0));
        var c = new VariableDefinitionNode(
            new Location(1, 1, 1, 1),
            new VariableNode("aa"),
            new NamedTypeNode("bb"),
            default,
            new List<DirectiveNode>(0));
        var d = new VariableDefinitionNode(
            new Location(2, 2, 2, 2),
            new VariableNode("aa"),
            new NamedTypeNode("bb"),
            default,
            new List<DirectiveNode>(0));

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

    [Fact]
    public void Create_VariableIsNull_ArgumentNullException()
    {
        // arrange
        // act
        void Action()
            => new VariableDefinitionNode(
                new Location(1, 1, 1, 1),
                null!,
                new NamedTypeNode(new NameNode("foo")),
                new StringValueNode("Foo"),
                new List<DirectiveNode>());

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public void Create_TypeIsNull_ArgumentNullException()
    {
        // arrange
        // act
        void Action()
            => new VariableDefinitionNode(
                new Location(1, 1, 1, 1),
                new VariableNode(new NameNode("foo")),
                null!,
                new StringValueNode("Foo"),
                new List<DirectiveNode>());

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public void Create_DirectivesIsNull_ArgumentNullException()
    {
        // arrange
        // act
        void Action()
            => new VariableDefinitionNode(
                new Location(1, 1, 1, 1),
                new VariableNode(new NameNode("foo")),
                new NamedTypeNode(new NameNode("foo")),
                new StringValueNode("Foo"),
                null!);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public void Create_ArgumentsArePassedCorrectly()
    {
        // arrange
        // act
        var variableDefinition = new VariableDefinitionNode(
            new Location(1, 2, 3, 5),
            new VariableNode(new NameNode("foo")),
            new NamedTypeNode(new NameNode("bar")),
            new StringValueNode("baz"),
            new List<DirectiveNode> { new("qux"), });

        // assert
        variableDefinition.MatchSnapshot();
    }

    [Fact]
    public void WithLocation()
    {
        // arrange
        var variableDefinition = new VariableDefinitionNode(
            new Location(1, 2, 3, 5),
            new VariableNode(new NameNode("foo")),
            new NamedTypeNode(new NameNode("bar")),
            new StringValueNode("baz"),
            new List<DirectiveNode> { new("qux"), });

        // act
        variableDefinition =
            variableDefinition.WithLocation(
                new Location(6, 7, 8, 9));

        // assert
        variableDefinition.MatchSnapshot();
    }

    [Fact]
    public void WithVariable()
    {
        // arrange
        var variableDefinition = new VariableDefinitionNode(
            new Location(1, 2, 3, 5),
            new VariableNode(new NameNode("foo")),
            new NamedTypeNode(new NameNode("bar")),
            new StringValueNode("baz"),
            new List<DirectiveNode>
            {
                    new DirectiveNode("qux"),
            });

        // act
        variableDefinition =
            variableDefinition.WithVariable(
                new VariableNode(new NameNode("quux")));

        // assert
        variableDefinition.MatchSnapshot();
    }

    [Fact]
    public void WithVariable_VariableIsNull_ArgumentException()
    {
        // arrange
        var variableDefinition = new VariableDefinitionNode(
            new Location(1, 2, 3, 5),
            new VariableNode(new NameNode("foo")),
            new NamedTypeNode(new NameNode("bar")),
            new StringValueNode("baz"),
            new List<DirectiveNode>
            {
                    new DirectiveNode("qux"),
            });

        // act
        void Action() => variableDefinition.WithVariable(null!);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public void WithType()
    {
        // arrange
        var variableDefinition = new VariableDefinitionNode(
            new Location(1, 2, 3, 5),
            new VariableNode(new NameNode("foo")),
            new NamedTypeNode(new NameNode("bar")),
            new StringValueNode("baz"),
            new List<DirectiveNode> { new("qux"), });

        // act
        variableDefinition =
            variableDefinition.WithType(
                new NamedTypeNode(new NameNode("quux")));

        // assert
        variableDefinition.MatchSnapshot();
    }

    [Fact]
    public void WithType_TypeIsNull_ArgumentException()
    {
        // arrange
        var variableDefinition = new VariableDefinitionNode(
            new Location(1, 2, 3, 5),
            new VariableNode(new NameNode("foo")),
            new NamedTypeNode(new NameNode("bar")),
            new StringValueNode("baz"),
            new List<DirectiveNode>
            {
                    new DirectiveNode("qux"),
            });

        // act
        void Action() => variableDefinition.WithType(null!);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public void WithDefaultValue()
    {
        // arrange
        var variableDefinition = new VariableDefinitionNode(
            new Location(1, 2, 3, 5),
            new VariableNode(new NameNode("foo")),
            new NamedTypeNode(new NameNode("bar")),
            new StringValueNode("baz"),
            new List<DirectiveNode> { new("qux"), });

        // act
        variableDefinition =
            variableDefinition.WithDefaultValue(
                new StringValueNode("quux"));

        // assert
        variableDefinition.MatchSnapshot();
    }

    [Fact]
    public void WithDirectives()
    {
        // arrange
        var variableDefinition = new VariableDefinitionNode(
            new Location(1, 2, 3, 5),
            new VariableNode(new NameNode("foo")),
            new NamedTypeNode(new NameNode("bar")),
            new StringValueNode("baz"),
            new List<DirectiveNode> { new("qux"), });

        // act
        variableDefinition =
            variableDefinition.WithDirectives(
                new List<DirectiveNode> { new("quux"), });

        // assert
        variableDefinition.MatchSnapshot();
    }

    [Fact]
    public void WithDirectives_TypeIsNull_ArgumentException()
    {
        // arrange
        var variableDefinition = new VariableDefinitionNode(
            new Location(1, 2, 3, 5),
            new VariableNode(new NameNode("foo")),
            new NamedTypeNode(new NameNode("bar")),
            new StringValueNode("baz"),
            new List<DirectiveNode> { new("qux"), });

        // act
        void Action() => variableDefinition.WithDirectives(null!);

        // assert
        Assert.Throws<ArgumentNullException>((Action) Action);
    }
}
