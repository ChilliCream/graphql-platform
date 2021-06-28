using System;
using System.Collections.Immutable;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Stitching.Delegation.ScopedVariables;
using HotChocolate.Types;
using Moq;
using Xunit;

namespace HotChocolate.Stitching.Delegation
{
    public class ScopedContextDataScopedVariableResolverTests
    {
        [Fact]
        public void CreateVariableValue()
        {
            // arrange
            var schema = Schema.Create(
                "type Query { foo(a: String = \"bar\") : String }",
                c =>
                {
                    c.Use(next => context => default);
                    c.Options.StrictValidation = false;
                });

            var contextData = ImmutableDictionary<string, object>.Empty
                .Add("a", "AbcDef");

            var context = new Mock<IResolverContext>(MockBehavior.Strict);
            context.SetupGet(t => t.ScopedContextData).Returns(contextData);

            var scopedVariable = new ScopedVariableNode(
                null,
                new NameNode("scopedContextData"),
                new NameNode("a"));

            // act
            var resolver = new ScopedContextDataScopedVariableResolver();
            ScopedVariableValue value = resolver.Resolve(
                context.Object,
                scopedVariable,
                schema.GetType<StringType>("String"));

            // assert
            Assert.Null(value.DefaultValue);
            Assert.Equal("__scopedContextData_a", value.Name);
            Assert.Equal("String", Assert.IsType<NamedTypeNode>(value.Type).Name.Value);
            Assert.Equal("AbcDef", value.Value!.Value);
        }

        [Fact]
        public void ContextDataEntryDoesNotExist()
        {
            // arrange
            var schema = Schema.Create(
                "type Query { foo(a: String = \"bar\") : String }",
                c =>
                {
                    c.Use(next => context => default);
                    c.Options.StrictValidation = false;
                });

            ImmutableDictionary<string, object> contextData =
                ImmutableDictionary<string, object>.Empty;

            var context = new Mock<IResolverContext>(MockBehavior.Strict);
            context.SetupGet(t => t.ScopedContextData).Returns(contextData);

            var scopedVariable = new ScopedVariableNode(
                null,
                new NameNode("scopedContextData"),
                new NameNode("a"));

            // act
            var resolver = new ScopedContextDataScopedVariableResolver();
            ScopedVariableValue value = resolver.Resolve(
                context.Object,
                scopedVariable,
                schema.GetType<StringType>("String"));

            // assert
            Assert.Null(value.DefaultValue);
            Assert.Equal("__scopedContextData_a", value.Name);
            Assert.Equal("String", Assert.IsType<NamedTypeNode>(value.Type).Name.Value);
            Assert.Equal(NullValueNode.Default, value.Value);
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
                new NameNode("scopedContextData"),
                new NameNode("b"));

            // act
            var resolver = new ScopedContextDataScopedVariableResolver();
            void Action() => resolver.Resolve(
                null!,
                scopedVariable,
                schema.GetType<StringType>("String"));

            // assert
            Assert.Equal("context", Assert.Throws<ArgumentNullException>(Action).ParamName);
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

            // act
            var resolver = new ScopedContextDataScopedVariableResolver();
            void Action() => resolver.Resolve(
                context.Object,
                null!,
                schema.GetType<StringType>("String"));

            // assert
            Assert.Equal("variable", Assert.Throws<ArgumentNullException>(Action).ParamName);
        }

        [Fact]
        public void TargetTypeIsNull()
        {
            // arrange
            var context = new Mock<IMiddlewareContext>();

            var scopedVariable = new ScopedVariableNode(
                null,
                new NameNode("scopedContextData"),
                new NameNode("b"));

            // act
            var resolver = new ScopedContextDataScopedVariableResolver();
            void Action() => resolver.Resolve(
                context.Object,
                scopedVariable,
                null!);

            // assert
            Assert.Equal("targetType", Assert.Throws<ArgumentNullException>(Action).ParamName);
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

            var scopedVariable = new ScopedVariableNode(
                null,
                new NameNode("foo"),
                new NameNode("b"));

            // act
            var resolver = new ScopedContextDataScopedVariableResolver();
            void Action() => resolver.Resolve(
                context.Object,
                scopedVariable,
                schema.GetType<StringType>("String"));

            // assert
            Assert.Equal("variable", Assert.Throws<ArgumentException>(Action).ParamName);
        }
    }
}
