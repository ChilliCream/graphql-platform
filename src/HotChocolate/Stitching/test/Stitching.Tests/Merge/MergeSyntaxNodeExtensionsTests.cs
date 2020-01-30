using System;
using HotChocolate.Language;
using HotChocolate.Stitching.Delegation;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Stitching.Merge
{
    public class MergeSyntaxNodeExtensionsTests
    {
        [Fact]
        public void AddDelegationPath_SingleComponent()
        {
            // arrange
            var fieldNode = new FieldDefinitionNode(
                null,
                new NameNode("foo"),
                null,
                Array.Empty<InputValueDefinitionNode>(),
                new NamedTypeNode(new NameNode("Type")),
                Array.Empty<DirectiveNode>());

            // act
            var path = new SelectionPathComponent(
                new NameNode("bar"),
                new[]
                {
                    new ArgumentNode("baz",
                        new ScopedVariableNode(
                            null,
                            new NameNode("qux"),
                            new NameNode("quux")))
                });

            fieldNode = fieldNode.AddDelegationPath("schemName", path);

            // assert
            var schema = new DocumentNode(new[]
                {
                    new ObjectTypeDefinitionNode
                    (
                        null,
                        new NameNode("Object"),
                        null,
                        Array.Empty<DirectiveNode>(),
                        Array.Empty<NamedTypeNode>(),
                        new[] { fieldNode }
                    )
                });
            SchemaSyntaxSerializer.Serialize(schema).MatchSnapshot();
        }

        [Fact]
        public void AddDelegationPath_SingleComponent_TwoArgs()
        {
            // arrange
            var fieldNode = new FieldDefinitionNode(
                null,
                new NameNode("foo"),
                null,
                Array.Empty<InputValueDefinitionNode>(),
                new NamedTypeNode(new NameNode("Type")),
                Array.Empty<DirectiveNode>());

            // act
            var path = new SelectionPathComponent(
                new NameNode("bar"),
                new[]
                {
                    new ArgumentNode("baz",
                        new ScopedVariableNode(
                            null,
                            new NameNode("qux"),
                            new NameNode("quux"))),
                    new ArgumentNode("value_arg", "value")
                });

            fieldNode = fieldNode.AddDelegationPath("schemName", path);

            // assert
            var schema = new DocumentNode(new[]
                {
                    new ObjectTypeDefinitionNode
                    (
                        null,
                        new NameNode("Object"),
                        null,
                        Array.Empty<DirectiveNode>(),
                        Array.Empty<NamedTypeNode>(),
                        new[] { fieldNode }
                    )
                });
            SchemaSyntaxSerializer.Serialize(schema).MatchSnapshot();
        }

        [Fact]
        public void AddDelegationPath_SingleComponent_SchemNameIsEmpty()
        {
            // arrange
            var fieldNode = new FieldDefinitionNode(
                null,
                new NameNode("foo"),
                null,
                Array.Empty<InputValueDefinitionNode>(),
                new NamedTypeNode(new NameNode("Type")),
                Array.Empty<DirectiveNode>());

            // act
            var path = new SelectionPathComponent(
                new NameNode("bar"),
                new[]
                {
                    new ArgumentNode("baz",
                        new ScopedVariableNode(
                            null,
                            new NameNode("qux"),
                            new NameNode("quux")))
                });

            Action action = () => fieldNode.AddDelegationPath(default, path);

            // assert
            Assert.Throws<ArgumentException>(action);
        }

        [Fact]
        public void AddDelegationPath_SingleComponent_PathIsNull()
        {
            // arrange
            var fieldNode = new FieldDefinitionNode(
                null,
                new NameNode("foo"),
                null,
                Array.Empty<InputValueDefinitionNode>(),
                new NamedTypeNode(new NameNode("Type")),
                Array.Empty<DirectiveNode>());

            // act
            Action action = () => fieldNode.AddDelegationPath(
                "Schema", (SelectionPathComponent)null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddDelegationPath_MultipleComponents()
        {
            // arrange
            var fieldNode = new FieldDefinitionNode(
                null,
                new NameNode("foo"),
                null,
                Array.Empty<InputValueDefinitionNode>(),
                new NamedTypeNode(new NameNode("Type")),
                Array.Empty<DirectiveNode>());

            // act
            var a = new SelectionPathComponent(
                new NameNode("bar"),
                new[]
                {
                    new ArgumentNode("baz",
                        new ScopedVariableNode(
                            null,
                            new NameNode("qux"),
                            new NameNode("quux")))
                });

            var b = new SelectionPathComponent(
                new NameNode("bar2"),
                new[]
                {
                    new ArgumentNode("baz2",
                        new ScopedVariableNode(
                            null,
                            new NameNode("qux2"),
                            new NameNode("quux2")))
                });

            fieldNode = fieldNode.AddDelegationPath(
                "schemName", new[] { a, b });

            // assert
            var schema = new DocumentNode(new[]
                {
                    new ObjectTypeDefinitionNode
                    (
                        null,
                        new NameNode("Object"),
                        null,
                        Array.Empty<DirectiveNode>(),
                        Array.Empty<NamedTypeNode>(),
                        new[] { fieldNode }
                    )
                });
            SchemaSyntaxSerializer.Serialize(schema).MatchSnapshot();
        }

        [Fact]
        public void AddDelegationPath_MultipleComponents_SingleComponent()
        {
            // arrange
            var fieldNode = new FieldDefinitionNode(
                null,
                new NameNode("foo"),
                null,
                Array.Empty<InputValueDefinitionNode>(),
                new NamedTypeNode(new NameNode("Type")),
                Array.Empty<DirectiveNode>());

            // act
            var path = new SelectionPathComponent(
                new NameNode("bar"),
                new[]
                {
                    new ArgumentNode("baz",
                        new ScopedVariableNode(
                            null,
                            new NameNode("qux"),
                            new NameNode("quux")))
                });

            fieldNode = fieldNode.AddDelegationPath(
                "schemName", new[] { path });

            // assert
            var schema = new DocumentNode(new[]
                {
                    new ObjectTypeDefinitionNode
                    (
                        null,
                        new NameNode("Object"),
                        null,
                        Array.Empty<DirectiveNode>(),
                        Array.Empty<NamedTypeNode>(),
                        new[] { fieldNode }
                    )
                });
            SchemaSyntaxSerializer.Serialize(schema).MatchSnapshot();
        }

        [Fact]
        public void AddDelegationPath_MultipleComponents_EmptyPath()
        {
            // arrange
            var fieldNode = new FieldDefinitionNode(
                null,
                new NameNode("foo"),
                null,
                Array.Empty<InputValueDefinitionNode>(),
                new NamedTypeNode(new NameNode("Type")),
                Array.Empty<DirectiveNode>());

            // act
            var path = new SelectionPathComponent(
                new NameNode("bar"),
                new[]
                {
                    new ArgumentNode("baz",
                        new ScopedVariableNode(
                            null,
                            new NameNode("qux"),
                            new NameNode("quux")))
                });

            fieldNode = fieldNode.AddDelegationPath(
                "schemName", Array.Empty<SelectionPathComponent>());

            // assert
            var schema = new DocumentNode(new[]
                {
                    new ObjectTypeDefinitionNode
                    (
                        null,
                        new NameNode("Object"),
                        null,
                        Array.Empty<DirectiveNode>(),
                        Array.Empty<NamedTypeNode>(),
                        new[] { fieldNode }
                    )
                });
            SchemaSyntaxSerializer.Serialize(schema).MatchSnapshot();
        }

        [Fact]
        public void AddDelegationPath_MultipleComponents_SchemNameIsEmpty()
        {
            // arrange
            var fieldNode = new FieldDefinitionNode(
                null,
                new NameNode("foo"),
                null,
                Array.Empty<InputValueDefinitionNode>(),
                new NamedTypeNode(new NameNode("Type")),
                Array.Empty<DirectiveNode>());

            // act
            var path = new SelectionPathComponent(
                new NameNode("bar"),
                new[]
                {
                    new ArgumentNode("baz",
                        new ScopedVariableNode(
                            null,
                            new NameNode("qux"),
                            new NameNode("quux")))
                });

            Action action = () => fieldNode.AddDelegationPath(
                default, new[] { path });

            // assert
            Assert.Throws<ArgumentException>(action);
        }

        [Fact]
        public void AddDelegationPath_MultipleComponents_PathIsNull()
        {
            // arrange
            var fieldNode = new FieldDefinitionNode(
                null,
                new NameNode("foo"),
                null,
                Array.Empty<InputValueDefinitionNode>(),
                new NamedTypeNode(new NameNode("Type")),
                Array.Empty<DirectiveNode>());

            // act
            Action action = () => fieldNode.AddDelegationPath(
                "Schema", (SelectionPathComponent[])null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddDelegationPath_Overwrite()
        {
            // arrange
            var fieldNode = new FieldDefinitionNode(
                null,
                new NameNode("foo"),
                null,
                Array.Empty<InputValueDefinitionNode>(),
                new NamedTypeNode(new NameNode("Type")),
                Array.Empty<DirectiveNode>());

            var path = new SelectionPathComponent(
                new NameNode("bar"),
                new[]
                {
                    new ArgumentNode("baz",
                        new ScopedVariableNode(
                            null,
                            new NameNode("qux"),
                            new NameNode("quux")))
                });

            fieldNode = fieldNode.AddDelegationPath("schemName", path);
            Assert.Collection(fieldNode.Directives,
                d => Assert.Equal("delegate", d.Name.Value));

            // act
            fieldNode = fieldNode.AddDelegationPath(
                "schemaName",
                "$ContextData:fooBar",
                true);

            // assert
            var schema = new DocumentNode(new[]
                {
                    new ObjectTypeDefinitionNode
                    (
                        null,
                        new NameNode("Object"),
                        null,
                        Array.Empty<DirectiveNode>(),
                        Array.Empty<NamedTypeNode>(),
                        new[] { fieldNode }
                    )
                });
            SchemaSyntaxSerializer.Serialize(schema).MatchSnapshot();
        }
    }
}
