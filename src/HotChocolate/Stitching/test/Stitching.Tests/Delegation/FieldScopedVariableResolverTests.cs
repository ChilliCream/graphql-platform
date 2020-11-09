using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Stitching.Delegation.ScopedVariables;
using HotChocolate.Types;
using Moq;
using Xunit;

namespace HotChocolate.Stitching.Delegation
{
    public class FieldScopedVariableResolverTests
    {
        [Fact]
        public void CreateVariableValue()
        {
            // arrange
            var schema = Schema.Create(
                "type Query { foo(a: String = \"bar\") : String a: String }",
                c =>
                {
                    c.Use(next => context => default);
                    c.Options.StrictValidation = false;
                });

            var context = new Mock<IResolverContext>(MockBehavior.Strict);
            context.SetupGet(t => t.ObjectType).Returns(
                schema.GetType<ObjectType>("Query"));
            context.SetupGet(t => t.Field).Returns(
                schema.GetType<ObjectType>("Query").Fields["foo"]);
            context.Setup(t => t.Parent<object>())
                .Returns(new Dictionary<string, object> { { "a", "baz" } });

            var scopedVariable = new ScopedVariableNode(
                null,
                new NameNode("fields"),
                new NameNode("a"));

            // act
            var resolver = new FieldScopedVariableResolver();
            VariableValue value = resolver.Resolve(
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
        public void FieldDoesNotExist()
        {
            // arrange
            var schema = Schema.Create(
                "type Query { foo(a: String = \"bar\") : String }",
                c =>
                {
                    c.Use(next => context => default);
                    c.Options.StrictValidation = false;
                });

            var context = new Mock<IResolverContext>(MockBehavior.Strict);
            context.SetupGet(t => t.ObjectType).Returns(
                schema.GetType<ObjectType>("Query"));
            context.SetupGet(t => t.Field).Returns(
                schema.GetType<ObjectType>("Query").Fields["foo"]);
            context.Setup(t => t.Parent<IReadOnlyDictionary<string, object>>())
                .Returns(new Dictionary<string, object> { { "a", "baz" } });
            context.Setup(t => t.FieldSelection)
                .Returns(new FieldNode(
                    null,
                    new NameNode("foo"),
                    null,
                    Array.Empty<DirectiveNode>(),
                    Array.Empty<ArgumentNode>(),
                    null));
            context.Setup(t => t.Path).Returns(Path.New("foo"));

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
            var schema = Schema.Create(
                "type Query { foo(a: String = \"bar\") : String }",
                c =>
                {
                    c.Use(next => context => default);
                    c.Options.StrictValidation = false;
                });

            var scopedVariable = new ScopedVariableNode(
                null,
                new NameNode("fields"),
                new NameNode("b"));

            // act
            var resolver = new FieldScopedVariableResolver();
            Action a = () => resolver.Resolve(
                null,
                scopedVariable,
                schema.GetType<StringType>("String"));

            // assert
            Assert.Equal("context", Assert.Throws<ArgumentNullException>(a).ParamName);
        }

        [Fact]
        public void ScopedVariableIsNull()
        {
            // arrange
            var schema = Schema.Create(
                "type Query { foo(a: String = \"bar\") : String }",
                c =>
                {
                    c.Use(next => context => default);
                    c.Options.StrictValidation = false;
                });

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
        public void InvalidScope()
        {
            // arrange
            var schema = Schema.Create(
                "type Query { foo(a: String = \"bar\") : String }",
                c =>
                {
                    c.Use(next => context => default);
                    c.Options.StrictValidation = false;
                });

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
}
