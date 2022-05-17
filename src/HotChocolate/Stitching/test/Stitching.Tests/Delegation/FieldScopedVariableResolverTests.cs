using System;
using System.Collections.Generic;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Stitching.Processing.ScopedVariables;
using HotChocolate.Types;
using Moq;
using Xunit;

namespace HotChocolate.Stitching.Delegation;

public class FieldScopedVariableResolverTests
{
    [Fact]
    [Obsolete]
    public void CreateVariableValue()
    {
        // arrange
        var inputFormatter = new InputFormatter();

        ISchema schema = SchemaBuilder.New()
            .AddDocumentFromString("type Query { foo(a: String = \"bar\") : String a: String }")
            .Use(_ => _)
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        var context = new Mock<IResolverContext>(MockBehavior.Strict);
        context.SetupGet(t => t.ObjectType).Returns(
            schema.GetType<ObjectType>("Query"));
        context.SetupGet(t => t.Field).Returns(
            schema.GetType<ObjectType>("Query").Fields["foo"]);
        context.Setup(t => t.Parent<object>())
            .Returns(new Dictionary<string, object> { { "a", "baz" } });
        context.Setup(t => t.Service<InputFormatter>()).Returns(inputFormatter);

        var scopedVariable = new ScopedVariableNode(
            null,
            new NameNode("fields"),
            new NameNode("a"));

        // act
        var resolver = new FieldScopedVariableResolver();
        ScopedVariableValue value = resolver.Resolve(
            context.Object,
            scopedVariable,
            schema.GetType<StringType>("String"));

        // assert
        Assert.Null(value.DefaultValue);
        Assert.Equal("__fields_a", value.Name);
        Assert.IsType<NamedTypeNode>(value.Type);
        Assert.Equal("baz", value.Value.Value);
    }

    [Fact]
    [Obsolete]
    public void FieldDoesNotExist()
    {
        // arrange
        ISchema schema = SchemaBuilder.New()
            .AddDocumentFromString("type Query { foo(a: String = \"bar\") : String }")
            .Use(_ => _)
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        var context = new Mock<IResolverContext>(MockBehavior.Strict);
        context.SetupGet(t => t.ObjectType).Returns(
            schema.GetType<ObjectType>("Query"));
        context.SetupGet(t => t.Field).Returns(
            schema.GetType<ObjectType>("Query").Fields["foo"]);
        context.Setup(t => t.Parent<IReadOnlyDictionary<string, object>>())
            .Returns(new Dictionary<string, object> { { "a", "baz" } });
        context.Setup(t => t.Selection.SyntaxNode)
            .Returns(new FieldNode(
                null,
                new NameNode("foo"),
                null,
                null,
                Array.Empty<DirectiveNode>(),
                Array.Empty<ArgumentNode>(),
                null));
        context.Setup(t => t.Path).Returns(PathFactory.Instance.New("foo"));

        var scopedVariable = new ScopedVariableNode(
            null,
            new NameNode("fields"),
            new NameNode("b"));

        // act
        var resolver = new FieldScopedVariableResolver();
        Action a = () => resolver.Resolve(
            context.Object,
            scopedVariable,
            schema.GetType<StringType>("String"));

        // assert
        Assert.Collection(
            Assert.Throws<GraphQLException>(a).Errors,
            t => Assert.Equal(ErrorCodes.Stitching.FieldNotDefined, t.Code));
    }

    [Fact]
    public void ContextIsNull()
    {
        // arrange
        ISchema schema = SchemaBuilder.New()
            .AddDocumentFromString("type Query { foo(a: String = \"bar\") : String }")
            .Use(_ => _)
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        var scopedVariable = new ScopedVariableNode(
            null,
            new NameNode("fields"),
            new NameNode("b"));

        // act
        var resolver = new FieldScopedVariableResolver();
        void Action()
            => resolver.Resolve(null!, scopedVariable, schema.GetType<StringType>("String"));

        // assert
        Assert.Equal("context", Assert.Throws<ArgumentNullException>((Action)Action).ParamName);
    }

    [Fact]
    [Obsolete]
    public void ScopedVariableIsNull()
    {
        // arrange
        ISchema schema = SchemaBuilder.New()
            .AddDocumentFromString("type Query { foo(a: String = \"bar\") : String }")
            .Use(_ => _)
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        var context = new Mock<IMiddlewareContext>();
        context.SetupGet(t => t.Field).Returns(
            schema.GetType<ObjectType>("Query").Fields["foo"]);
        context.Setup(t => t.ArgumentValue<object>(It.IsAny<NameString>()))
            .Returns("Baz");

        // act
        var resolver = new FieldScopedVariableResolver();
        Action a = () => resolver.Resolve(
            context.Object,
            null,
            schema.GetType<StringType>("String"));

        // assert
        Assert.Equal("variable", Assert.Throws<ArgumentNullException>(a).ParamName);
    }

    [Fact]
    [Obsolete]
    public void InvalidScope()
    {
        // arrange
        ISchema schema = SchemaBuilder.New()
            .AddDocumentFromString("type Query { foo(a: String = \"bar\") : String }")
            .Use(_ => _)
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        var context = new Mock<IMiddlewareContext>();
        context.SetupGet(t => t.Field).Returns(
            schema.GetType<ObjectType>("Query").Fields["foo"]);
        context.Setup(t => t.ArgumentValue<object>(It.IsAny<NameString>()))
            .Returns("Baz");

        var scopedVariable = new ScopedVariableNode(
            null,
            new NameNode("foo"),
            new NameNode("b"));

        // act
        var resolver = new FieldScopedVariableResolver();
        Action a = () => resolver.Resolve(
            context.Object,
            scopedVariable,
            schema.GetType<StringType>("String"));

        // assert
        Assert.Equal("variable", Assert.Throws<ArgumentException>(a).ParamName);
    }
}
