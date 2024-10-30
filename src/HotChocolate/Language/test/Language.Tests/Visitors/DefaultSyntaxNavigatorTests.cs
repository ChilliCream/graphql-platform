using Xunit;
using static HotChocolate.Language.Utf8GraphQLParser.Syntax;

namespace HotChocolate.Language.Visitors;

public class DefaultSyntaxNavigatorTests
{
    [Fact]
    public void Push()
    {
        // arrange
        var node = new NameNode("abc");

        // act
        var navigator = new DefaultSyntaxNavigator();
        navigator.Push(node);

        // assert
        Assert.Equal(1, navigator.Count);
    }

    [Fact]
    public void Push_Two()
    {
        // arrange
        var node = new NameNode("abc");

        // act
        var navigator = new DefaultSyntaxNavigator();
        navigator.Push(node);
        navigator.Push(node);

        // assert
        Assert.Equal(2, navigator.Count);
    }

    [Fact]
    public void Push_Null()
    {
        // arrange
        // act
        var navigator = new DefaultSyntaxNavigator();
        void Fail() => navigator.Push(null!);

        // assert
        Assert.Throws<ArgumentNullException>(Fail);
    }

    [Fact]
    public void Pop()
    {
        // arrange
        var node = new NameNode("abc");
        var navigator = new DefaultSyntaxNavigator();
        navigator.Push(node);

        // act
        var popped = navigator.Pop();

        // assert
        Assert.Same(node, popped);
    }

    [Fact]
    public void Pop_Two()
    {
        // arrange
        var one = new NameNode("abc");
        var two = new NameNode("def");
        var navigator = new DefaultSyntaxNavigator();

        // act
        navigator.Push(one);
        navigator.Push(two);

        // assert
        var popped = navigator.Pop();
        Assert.Same(two, popped);

        popped = navigator.Pop();
        Assert.Same(one, popped);
    }

    [Fact]
    public void Pop_Empty()
    {
        // arrange
        // act
        var navigator = new DefaultSyntaxNavigator();
        void Fail() => navigator.Pop();

        // assert
        Assert.Throws<InvalidOperationException>(Fail);
    }

    [Fact]
    public void Peek()
    {
        // arrange
        var node = new NameNode("abc");
        var navigator = new DefaultSyntaxNavigator();
        navigator.Push(node);

        // act
        var peeked = navigator.Peek();

        // assert
        Assert.Same(node, peeked);
    }

    [Fact]
    public void Peek_Two()
    {
        // arrange
        var one = new NameNode("abc");
        var two = new NameNode("def");
        var navigator = new DefaultSyntaxNavigator();
        navigator.Push(one);
        navigator.Push(two);

        // act
        var peeked = navigator.Peek();

        // assert
        Assert.Same(two, peeked);
    }

    [Fact]
    public void Peek_Empty()
    {
        // arrange
        var navigator = new DefaultSyntaxNavigator();

        // act
        void Fail() => navigator.Peek();

        // assert
        Assert.Throws<InvalidOperationException>(Fail);
    }

    [Fact]
    public void Peek_Explicit_First()
    {
        // arrange
        var one = new NameNode("abc");
        var two = new NameNode("def");
        var three = new NameNode("ghi");
        var navigator = new DefaultSyntaxNavigator();
        navigator.Push(one);
        navigator.Push(two);
        navigator.Push(three);

        // act
        var peeked = navigator.Peek(0);

        // assert
        Assert.Same(three, peeked);
    }

    [Fact]
    public void Peek_Explicit_Two()
    {
        // arrange
        var one = new NameNode("abc");
        var two = new NameNode("def");
        var three = new NameNode("ghi");
        var navigator = new DefaultSyntaxNavigator();
        navigator.Push(one);
        navigator.Push(two);
        navigator.Push(three);

        // act
        var peeked = navigator.Peek(1);

        // assert
        Assert.Same(two, peeked);
    }

    [Fact]
    public void Peek_Explicit_Three()
    {
        // arrange
        var one = new NameNode("abc");
        var two = new NameNode("def");
        var three = new NameNode("ghi");
        var navigator = new DefaultSyntaxNavigator();
        navigator.Push(one);
        navigator.Push(two);
        navigator.Push(three);

        // act
        var peeked = navigator.Peek(2);

        // assert
        Assert.Same(one, peeked);
    }

    [Fact]
    public void Peek_Explicit_Four()
    {
        // arrange
        var one = new NameNode("abc");
        var two = new NameNode("def");
        var three = new NameNode("ghi");
        var navigator = new DefaultSyntaxNavigator();
        navigator.Push(one);
        navigator.Push(two);
        navigator.Push(three);

        // act
        void Fail() => navigator.Peek(3);

        // assert
        Assert.Throws<InvalidOperationException>(Fail);
    }

    [Fact]
    public void Peek_Explicit_Negative_Count()
    {
        // arrange
        var one = new NameNode("abc");
        var navigator = new DefaultSyntaxNavigator();
        navigator.Push(one);

        // act
        void Fail() => navigator.Peek(-1);

        // assert
        Assert.Throws<ArgumentOutOfRangeException>(Fail);
    }

    [Fact]
    public void GetAncestor()
    {
        // arrange
        var one = new NamedTypeNode(new NameNode("abc"));
        var two = new NameNode("def");
        var navigator = new DefaultSyntaxNavigator();
        navigator.Push(one);
        navigator.Push(two);

        // act
        var ancestor = navigator.GetAncestor<NamedTypeNode>();

        // assert
        Assert.Equal(one, ancestor);
    }

    [Fact]
    public void GetAncestor_NotFound()
    {
        // arrange
        var one = new NamedTypeNode(new NameNode("abc"));
        var two = new NameNode("def");
        var navigator = new DefaultSyntaxNavigator();
        navigator.Push(one);
        navigator.Push(two);

        // act
        var ancestor = navigator.GetAncestor<ObjectTypeDefinitionNode>();

        // assert
        Assert.Null(ancestor);
    }

    [Fact]
    public void GetAncestors()
    {
        // arrange
        var one = new NamedTypeNode(new NameNode("abc"));
        var two = new NameNode("def");
        var three = new NamedTypeNode(new NameNode("ghi"));
        var four = new NameNode("jkl");
        var navigator = new DefaultSyntaxNavigator();
        navigator.Push(one);
        navigator.Push(two);
        navigator.Push(three);
        navigator.Push(four);

        // act
        var ancestors = navigator.GetAncestors<NamedTypeNode>();

        // assert
        Assert.Collection(
            ancestors,
            t => Assert.Same(three, t),
            t => Assert.Same(one, t));
    }

    [Fact]
    public void GetAncestors_Empty()
    {
        // arrange
        // act
        var navigator = new DefaultSyntaxNavigator();

        // assert
        Assert.Empty(navigator.GetAncestors<ISyntaxNode>());
    }

    [Fact]
    public void CreateCoordinate_Directive()
    {
        // arrange
        var one = new NamedTypeNode(new NameNode("abc"));
        var two = new NameNode("def");
        var three = new NamedTypeNode(new NameNode("ghi"));
        var four = new NameNode("jkl");

        var directive =
            ParseDirectiveDefinition("directive @foo(arg: String!) on FIELD_DEFINITION");
        var argument = directive.Arguments.First();

        var navigator = new DefaultSyntaxNavigator();
        navigator.Push(one);
        navigator.Push(directive);
        navigator.Push(two);
        navigator.Push(argument);
        navigator.Push(three);
        navigator.Push(four);

        // act
        var coordinate = navigator.CreateCoordinate();

        // assert
        Assert.Equal("@foo(arg:)", coordinate.ToString());
    }

    [Fact]
    public void CreateCoordinate_ObjectTypeDefinition_1()
    {
        // arrange
        var one = new NamedTypeNode(new NameNode("abc"));
        var two = new NameNode("def");
        var three = new NamedTypeNode(new NameNode("ghi"));
        var four = new NameNode("jkl");

        var type = ParseObjectTypeDefinition(
            "type Foo { bar(baz: String): Int }");
        var field = type.Fields[0];
        var argument = field.Arguments[0];

        var navigator = new DefaultSyntaxNavigator();
        navigator.Push(one);
        navigator.Push(type);
        navigator.Push(two);
        navigator.Push(field);
        navigator.Push(three);
        navigator.Push(argument);
        navigator.Push(four);

        // act
        var coordinate = navigator.CreateCoordinate();

        // assert
        Assert.Equal("Foo.bar(baz:)", coordinate.ToString());
    }

    [Fact]
    public void CreateCoordinate_ObjectTypeDefinition_2()
    {
        // arrange
        var one = new NamedTypeNode(new NameNode("abc"));
        var two = new NameNode("def");
        var three = new NamedTypeNode(new NameNode("ghi"));

        var type = ParseObjectTypeDefinition(
            "type Foo { bar(baz: String): Int }");
        var field = type.Fields[0];

        var navigator = new DefaultSyntaxNavigator();
        navigator.Push(one);
        navigator.Push(type);
        navigator.Push(two);
        navigator.Push(field);
        navigator.Push(three);

        // act
        var coordinate = navigator.CreateCoordinate();

        // assert
        Assert.Equal("Foo.bar", coordinate.ToString());
    }

    [Fact]
    public void CreateCoordinate_ObjectTypeDefinition_3()
    {
        // arrange
        var one = new NamedTypeNode(new NameNode("abc"));
        var two = new NameNode("def");

        var type = ParseObjectTypeDefinition(
            "type Foo { bar(baz: String): Int }");

        var navigator = new DefaultSyntaxNavigator();
        navigator.Push(one);
        navigator.Push(type);
        navigator.Push(two);

        // act
        var coordinate = navigator.CreateCoordinate();

        // assert
        Assert.Equal("Foo", coordinate.ToString());
    }

    [Fact]
    public void CreateCoordinate_Empty()
    {
        // arrange
        var navigator = new DefaultSyntaxNavigator();

        // act
        void Fail() => navigator.CreateCoordinate();

        // assert
        Assert.Throws<InvalidOperationException>(Fail);
    }

    [Fact]
    public void CreateCoordinate_Invalid_1()
    {
        // arrange
        var navigator = new DefaultSyntaxNavigator();
        navigator.Push(new NameNode("abc"));

        // act
        void Fail() => navigator.CreateCoordinate();

        // assert
        Assert.Throws<InvalidOperationException>(Fail);
    }

    [Fact]
    public void CreateCoordinate_Invalid_2()
    {
        // arrange
        var navigator = new DefaultSyntaxNavigator();
        navigator.Push(new NameNode("abc"));
        navigator.Push(ParseFieldDefinition("a: String"));

        // act
        void Fail() => navigator.CreateCoordinate();

        // assert
        Assert.Throws<InvalidOperationException>(Fail);
    }
}
