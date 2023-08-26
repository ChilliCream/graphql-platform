using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Stitching.Delegation.ScopedVariables;
using HotChocolate.Types;
using Moq;

namespace HotChocolate.Stitching.Delegation;

public class ArgumentScopedVariableResolverTests
{
    [Fact]
    public void CreateVariableValue()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddDocumentFromString("type Query { foo(a: String = \"bar\") : String }")
            .Use(_ => _)
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        var field = schema.GetType<ObjectType>("Query").Fields["foo"];

        var selection = new Mock<ISelection>(MockBehavior.Strict);
        selection.SetupGet(t => t.Field).Returns(field);

        var context = new Mock<IResolverContext>(MockBehavior.Strict);
        context.SetupGet(t => t.Selection).Returns(selection.Object);
        context.Setup(t => t.ArgumentLiteral<IValueNode>("a"))
            .Returns(new StringValueNode("baz"));

        var scopedVariable = new ScopedVariableNode(
            null,
            new NameNode("arguments"),
            new NameNode("a"));

        // act
        var resolver = new ArgumentScopedVariableResolver();
        var value = resolver.Resolve(
            context.Object,
            scopedVariable,
            schema.GetType<StringType>("String"));

        // assert
        Assert.Equal("bar", Assert.IsType<StringValueNode>(value.DefaultValue).Value);
        Assert.Equal("__arguments_a", value.Name);
        Assert.Equal("String", Assert.IsType<NamedTypeNode>(value.Type).Name.Value);
        Assert.Equal("baz", value.Value!.Value);
    }

    [Fact]
    public void ArgumentDoesNotExist()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddDocumentFromString("type Query { foo(a: String = \"bar\") : String }")
            .Use(_ => _)
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        var selection = new Mock<ISelection>(MockBehavior.Strict);
        selection.SetupGet(t => t.Field).Returns(
            schema.GetType<ObjectType>("Query").Fields["foo"]);

        var context = new Mock<IResolverContext>(MockBehavior.Strict);
        context.SetupGet(t => t.Selection).Returns(selection.Object);

        context.Setup(t => t.ArgumentValue<object>(It.IsAny<string>()))
            .Returns("Baz");
        context.Setup(t => t.Selection.SyntaxNode)
            .Returns(new FieldNode(
                null,
                new NameNode("foo"),
                null,
                null,
                Array.Empty<DirectiveNode>(),
                Array.Empty<ArgumentNode>(),
                null));
        context.Setup(t => t.Path).Returns(Path.Root.Append("foo"));

        var scopedVariable = new ScopedVariableNode(
            null,
            new NameNode("arguments"),
            new NameNode("b"));

        // act
        var resolver = new ArgumentScopedVariableResolver();
        Action a = () => resolver.Resolve(
            context.Object,
            scopedVariable,
            schema.GetType<StringType>("String"));

        // assert
        Assert.Collection(
            Assert.Throws<GraphQLException>(a).Errors,
            t => Assert.Equal(ErrorCodes.Stitching.ArgumentNotDefined, t.Code));
    }

    [Fact]
    public void ContextIsNull()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddDocumentFromString("type Query { foo(a: String = \"bar\") : String }")
            .Use(_ => _)
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        var scopedVariable = new ScopedVariableNode(
            null,
            new NameNode("arguments"),
            new NameNode("b"));

        // act
        var resolver = new ArgumentScopedVariableResolver();
        Action a = () => resolver.Resolve(
            null!,
            scopedVariable,
            schema.GetType<StringType>("String"));

        // assert
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(a).ParamName);
    }

    [Fact]
    public void ScopedVariableIsNull()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddDocumentFromString("type Query { foo(a: String = \"bar\") : String }")
            .Use(_ => _)
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        var selection = new Mock<ISelection>(MockBehavior.Strict);
        selection.SetupGet(t => t.Field).Returns(
            schema.GetType<ObjectType>("Query").Fields["foo"]);

        var context = new Mock<IResolverContext>(MockBehavior.Strict);
        context.SetupGet(t => t.Selection).Returns(selection.Object);
        context.Setup(t => t.ArgumentValue<object>(It.IsAny<string>()))
            .Returns("Baz");

        // act
        var resolver = new ArgumentScopedVariableResolver();
        Action a = () => resolver.Resolve(
            context.Object,
            null!,
            schema.GetType<StringType>("String"));

        // assert
        Assert.Equal("variable", Assert.Throws<ArgumentNullException>(a).ParamName);
    }

    [Fact]
    public void InvalidScope()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddDocumentFromString("type Query { foo(a: String = \"bar\") : String }")
            .Use(_ => _)
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        var field = schema.GetType<ObjectType>("Query").Fields["foo"];

        var selection = new Mock<ISelection>(MockBehavior.Strict);
        selection.SetupGet(t => t.Field).Returns(field);

        var context = new Mock<IResolverContext>(MockBehavior.Strict);
        context.SetupGet(t => t.Selection).Returns(selection.Object);
        context.Setup(t => t.ArgumentValue<object>(It.IsAny<string>())).Returns("Baz");

        var scopedVariable = new ScopedVariableNode(
            null,
            new NameNode("foo"),
            new NameNode("b"));

        // act
        var resolver = new ArgumentScopedVariableResolver();
        Action a = () => resolver.Resolve(
            context.Object,
            scopedVariable,
            schema.GetType<StringType>("String"));

        // assert
        Assert.Equal("variable", Assert.Throws<ArgumentException>(a).ParamName);
    }
}
