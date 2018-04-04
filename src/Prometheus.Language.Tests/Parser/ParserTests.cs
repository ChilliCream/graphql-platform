using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Xunit;

namespace Prometheus.Language
{
    public class ParserTests
    {
        [Fact]
        public void ParserSimpleObjectType()
        {
            // arrange
            string sourceText = "type a { b: String c: Int }";
            Source source = new Source(sourceText);
            Lexer lexer = new Lexer();

            // act
            Parser parser = new Parser();
            DocumentNode document = parser.Parse(lexer, source);

            // assert
            Assert.Collection(document.Definitions,
                t =>
                {
                    Assert.IsType<ObjectTypeDefinitionNode>(t);
                    var objectTypeDefinition = (ObjectTypeDefinitionNode)t;
                    Assert.Equal(NodeKind.ObjectTypeDefinition, objectTypeDefinition.Kind);
                    Assert.Equal("a", objectTypeDefinition.Name.Value);
                    Assert.Collection(objectTypeDefinition.Fields,
                        f =>
                        {
                            Assert.Equal("b", f.Name.Value);
                            Assert.IsType<NamedTypeNode>(f.Type);
                            Assert.Equal("String", ((NamedTypeNode)f.Type).Name.Value);
                        },
                        f =>
                        {
                            Assert.Equal("c", f.Name.Value);
                            Assert.IsType<NamedTypeNode>(f.Type);
                            Assert.Equal("Int", ((NamedTypeNode)f.Type).Name.Value);
                        });
                });
        }

        [Fact]
        public void ParserSimpleInterfaceType()
        {
            // arrange
            string sourceText = "interface a { b: String c: Int }";
            Source source = new Source(sourceText);
            Lexer lexer = new Lexer();

            // act
            Parser parser = new Parser();
            DocumentNode document = parser.Parse(lexer, source);

            // assert
            Assert.Collection(document.Definitions,
                t =>
                {
                    Assert.IsType<InterfaceTypeDefinitionNode>(t);
                    var objectTypeDefinition = (InterfaceTypeDefinitionNode)t;
                    Assert.Equal(NodeKind.InterfaceTypeDefinition, objectTypeDefinition.Kind);
                    Assert.Equal("a", objectTypeDefinition.Name.Value);
                    Assert.Collection(objectTypeDefinition.Fields,
                        f =>
                        {
                            Assert.Equal("b", f.Name.Value);
                            Assert.IsType<NamedTypeNode>(f.Type);
                            Assert.Equal("String", ((NamedTypeNode)f.Type).Name.Value);
                        },
                        f =>
                        {
                            Assert.Equal("c", f.Name.Value);
                            Assert.IsType<NamedTypeNode>(f.Type);
                            Assert.Equal("Int", ((NamedTypeNode)f.Type).Name.Value);
                        });
                });
        }


        [Fact(Skip = "Not finished implementing this test")]
        public void ParseFacebookKitchenSinkSchema()
        {
            // arrange
            string sourceFile = Path.Combine(
                Path.GetDirectoryName(GetType().Assembly.Location),
                "Resources", "schema-kitchen-sink.graphql");
            string sourceText = File.ReadAllText(sourceFile);
            Source source = new Source(sourceText);

            // act
            Parser parser = new Parser();
            DocumentNode document = parser.Parse(source);

            // assert
            Assert.Collection(
                document.Definitions,
                SchemaAsserts,
                FooTypeAsserts,
                AnnotatedObjectTypeAsserts,
                UndefinedTypeTypeAsserts,
                FooTypeExtensionAsserts,
                FooTypeExtensionWithDirectiveAsserts,
                BarInterfaceAsserts,
                AnnotatedInterfaceAsserts,
                UndefinedInterfaceAsserts,
                BarInterfaceExtensionAsserts,
                BarInterfaceExtensionWithDirectiveAsserts,
                FeedUnionAsserts,
                AnnotatedUnionAsserts,
                AnnotatedUnionTwoAsserts,
                UndefinedUnionAsserts,
                FeedUnionExtensionAsserts,
                FeedUnionExtensionWithDirectivesAsserts,
                CustomScalarAsserts,
                AnnotatedScalarAsserts,
                CustomScalarExtensionAsserts,
                SiteEnumAsserts,
                AnnotatedEnumWithDirectivesAsserts,
                UndefinedEnumAsserts,
                SiteEnumExtensionAsserts,
                SiteEnumExtensionWithDirectiveAsserts,
                InputTypeAsserts,
                AnnotatedInputAsserts,
                UndefinedInputAsserts,
                InputTypeExtensionAsserts,
                InputTypeExtensionWithDirectivesAsserts,
                SkipDirectiveAsserts,
                IncludeDirectiveAsserts,
                Include2DirectiveAsserts);
        }

        private void SchemaAsserts(IDefinitionNode definitionNode)
        {
            var schema = SyntaxNodeAsserts<SchemaDefinitionNode>(
                NodeKind.SchemaDefinition, definitionNode);
            Assert.Empty(schema.Directives);

            Assert.Collection(schema.OperationTypes,
                t =>
                {
                    SyntaxNodeAsserts(NodeKind.OperationTypeDefinition, t);
                    NamedTypeAsserts("QueryType", t.Type);
                    Assert.Equal(OperationType.Query, t.Operation);
                },
                t =>
                {
                    SyntaxNodeAsserts(NodeKind.OperationTypeDefinition, t);
                    NamedTypeAsserts("MutationType", t.Type);
                    Assert.Equal(OperationType.Mutation, t.Operation);
                });
        }

        private void FooTypeAsserts(IDefinitionNode definitionNode)
        {
            var type = SyntaxNodeAsserts<ObjectTypeDefinitionNode>(
                NodeKind.ObjectTypeDefinition, definitionNode);
            Assert.Empty(type.Directives);
            NameAsserts("Foo", type.Name);
            Assert.Equal("This is a description\r\nof the `Foo` type.",
                type.Description.Value);

            Assert.Collection(type.Interfaces,
                t => NamedTypeAsserts("Bar", t),
                t => NamedTypeAsserts("Baz", t));

            Assert.Collection(type.Fields,
                t => FieldDefinitionAsserts("one", t));
        }

        private void AnnotatedObjectTypeAsserts(IDefinitionNode definitionNode)
        {
            // TODO : asserts
        }

        private void UndefinedTypeTypeAsserts(IDefinitionNode definitionNode)
        {
            // TODO : asserts
        }

        private void FooTypeExtensionAsserts(IDefinitionNode definitionNode)
        {
            // TODO : asserts
        }

        private void FooTypeExtensionWithDirectiveAsserts(IDefinitionNode definitionNode)
        {
            // TODO : asserts
        }

        private void BarInterfaceAsserts(IDefinitionNode definitionNode)
        {
            // TODO : asserts
        }

        private void AnnotatedInterfaceAsserts(IDefinitionNode definitionNode)
        {
            // TODO : asserts
        }

        private void UndefinedInterfaceAsserts(IDefinitionNode definitionNode)
        {
            // TODO : asserts
        }

        private void BarInterfaceExtensionAsserts(IDefinitionNode definitionNode)
        {
            // TODO : asserts
        }

        private void BarInterfaceExtensionWithDirectiveAsserts(IDefinitionNode definitionNode)
        {
            // TODO : asserts
        }

        private void FeedUnionAsserts(IDefinitionNode definitionNode)
        {
            // TODO : asserts
        }

        private void AnnotatedUnionAsserts(IDefinitionNode definitionNode)
        {
            // TODO : asserts
        }

        private void AnnotatedUnionTwoAsserts(IDefinitionNode definitionNode)
        {
            // TODO : asserts
        }

        private void UndefinedUnionAsserts(IDefinitionNode definitionNode)
        {
            // TODO : asserts
        }

        private void FeedUnionExtensionAsserts(IDefinitionNode definitionNode)
        {
            // TODO : asserts
        }

        private void FeedUnionExtensionWithDirectivesAsserts(IDefinitionNode definitionNode)
        {
            // TODO : asserts
        }

        private void CustomScalarAsserts(IDefinitionNode definitionNode)
        {
            // TODO : asserts
        }

        private void AnnotatedScalarAsserts(IDefinitionNode definitionNode)
        {
            // TODO : asserts
        }

        private void CustomScalarExtensionAsserts(IDefinitionNode definitionNode)
        {
            // TODO : asserts
        }

        private void SiteEnumAsserts(IDefinitionNode definitionNode)
        {
            // TODO : asserts
        }

        private void AnnotatedEnumWithDirectivesAsserts(IDefinitionNode definitionNode)
        {
            // TODO : asserts
        }

        private void UndefinedEnumAsserts(IDefinitionNode definitionNode)
        {
            // TODO : asserts
        }

        private void SiteEnumExtensionAsserts(IDefinitionNode definitionNode)
        {
            // TODO : asserts
        }

        private void SiteEnumExtensionWithDirectiveAsserts(IDefinitionNode definitionNode)
        {
            // TODO : asserts
        }

        private void InputTypeAsserts(IDefinitionNode definitionNode)
        {
            // TODO : asserts
        }

        private void AnnotatedInputAsserts(IDefinitionNode definitionNode)
        {
            // TODO : asserts
        }

        private void UndefinedInputAsserts(IDefinitionNode definitionNode)
        {
            // TODO : asserts
        }

        private void InputTypeExtensionAsserts(IDefinitionNode definitionNode)
        {
            // TODO : asserts
        }

        private void InputTypeExtensionWithDirectivesAsserts(IDefinitionNode definitionNode)
        {
            // TODO : asserts
        }

        private void SkipDirectiveAsserts(IDefinitionNode definitionNode)
        {
            // TODO : asserts
        }

        private void IncludeDirectiveAsserts(IDefinitionNode definitionNode)
        {
            // TODO : asserts
        }

        private void Include2DirectiveAsserts(IDefinitionNode definitionNode)
        {
            // TODO : asserts
        }



        private void FieldDefinitionAsserts(
            string expectedName, FieldDefinitionNode field,
            params Action<InputValueDefinitionNode>[] argumentInsepctors)
        {
            SyntaxNodeAsserts(NodeKind.FieldDefinition, field);
            NameAsserts(expectedName, field.Name);

            if (argumentInsepctors.Any())
            {
                Assert.Collection(field.Arguments, argumentInsepctors);
            }
            else
            {
                Assert.Empty(field.Arguments);
            }

        }

        private T SyntaxNodeAsserts<T>(NodeKind expectedNodeKind, ISyntaxNode node)
            where T : ISyntaxNode
        {
            Assert.NotNull(node);
            Assert.NotNull(node.Location);
            Assert.IsType<T>(node);
            Assert.Equal(expectedNodeKind, node.Kind);
            return (T)node;
        }

        private void SyntaxNodeAsserts(NodeKind expectedNodeKind, ISyntaxNode node)
        {
            Assert.NotNull(node);
            Assert.NotNull(node.Location);
            Assert.Equal(expectedNodeKind, node.Kind);
        }

        private void NamedTypeAsserts(string expectedName, NamedTypeNode namedTypeNode)
        {
            Assert.NotNull(namedTypeNode);
            Assert.NotNull(namedTypeNode.Location);
            Assert.NotNull(namedTypeNode.Name);
            Assert.NotNull(namedTypeNode.Name.Location);

            Assert.Equal(NodeKind.NamedType, namedTypeNode.Kind);
            Assert.Equal(NodeKind.Name, namedTypeNode.Name.Kind);

            Assert.Equal(expectedName, namedTypeNode.Name.Value);
        }

        private void NameAsserts(string expectedName, NameNode nameNode)
        {
            Assert.NotNull(nameNode);
            Assert.NotNull(nameNode.Location);
            Assert.Equal(NodeKind.Name, nameNode.Kind);

            Assert.Equal(expectedName, nameNode.Value);
        }

    }
}