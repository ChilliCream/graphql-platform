using System;
using System.Collections.Generic;
using Snapshooter.Xunit;
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
        var abResult = a.Equals(b);
        var aaResult = a.Equals(a);
        var acResult = a.Equals(c);
        var aNullResult = a.Equals(default);

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
        var abResult = a.Equals(b);
        var aaResult = a.Equals(a);
        var acResult = a.Equals(c);
        var aNullResult = a.Equals(default);

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
        var aHash = a.GetHashCode();
        var bHash = b.GetHashCode();
        var cHash = c.GetHashCode();
        var dHash = d.GetHashCode();

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
        Action action = () => new VariableDefinitionNode(
            new Location(1, 1, 1, 1),
            null,
            new NamedTypeNode(new NameNode("foo")),
            new StringValueNode("Foo"),
            new List<DirectiveNode>());

        // assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public void Create_TypeIsNull_ArgumentNullException()
    {
        // arrange
        // act
        Action action = () => new VariableDefinitionNode(
            new Location(1, 1, 1, 1),
            new VariableNode(new NameNode("foo")),
            null,
            new StringValueNode("Foo"),
            new List<DirectiveNode>());

        // assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public void Create_DirectivesIsNull_ArgumentNullException()
    {
        // arrange
        // act
        Action action = () => new VariableDefinitionNode(
            new Location(1, 1, 1, 1),
            new VariableNode(new NameNode("foo")),
            new NamedTypeNode(new NameNode("foo")),
            new StringValueNode("Foo"),
            null);

        // assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public void Create_ArgumentsArePassedCorrecctly()
    {
        // arrange
        // act
        var variableDefinition = new VariableDefinitionNode(
            new Location(1, 2, 3, 5),
            new VariableNode(new NameNode("foo")),
            new NamedTypeNode(new NameNode("bar")),
            new StringValueNode("baz"),
            new List<DirectiveNode>
            {
                    new DirectiveNode("qux")
            });

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
            new List<DirectiveNode>
            {
                    new DirectiveNode("qux")
            });

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
                    new DirectiveNode("qux")
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
                    new DirectiveNode("qux")
            });

        // act
        Action action = () =>
            variableDefinition.WithVariable(null);

        // assert
        Assert.Throws<ArgumentNullException>(action);
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
            new List<DirectiveNode>
            {
                    new DirectiveNode("qux")
            });

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
                    new DirectiveNode("qux")
            });

        // act
        Action action = () =>
            variableDefinition.WithType(null);

        // assert
        Assert.Throws<ArgumentNullException>(action);
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
            new List<DirectiveNode>
            {
                    new DirectiveNode("qux")
            });

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
            new List<DirectiveNode>
            {
                    new DirectiveNode("qux")
            });

        // act
        variableDefinition =
            variableDefinition.WithDirectives(
                new List<DirectiveNode>
                {
                        new DirectiveNode("quux")
                });

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
            new List<DirectiveNode>
            {
                    new DirectiveNode("qux")
            });

        // act
        Action action = () =>
            variableDefinition.WithDirectives(null);

        // assert
        Assert.Throws<ArgumentNullException>(action);
    }
}
