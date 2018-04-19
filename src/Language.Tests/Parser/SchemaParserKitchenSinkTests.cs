using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace HotChocolate.Language
{
    public class SchemaParserKitchenSinkTests
    {
        [Fact]
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
            DocumentNode document = parser.Parse(new Lexer(), source);

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
            NameAsserts("Foo", type.Name);
            Assert.Equal("This is a description\nof the `Foo` type.",
                type.Description.Value);

            Assert.Collection(type.Interfaces,
                t => NamedTypeAsserts("Bar", t),
                t => NamedTypeAsserts("Baz", t));

            Assert.Empty(type.Directives);

            Assert.Collection(type.Fields,
                t => FieldDefinitionAsserts("one", "Type", t),
                t => FieldDefinitionAsserts("two", "Type", t,
                    z => InputValueAsserts("argument", "InputType!", z)),
                t => FieldDefinitionAsserts("three", "Int", t,
                    z => InputValueAsserts("argument", "InputType", z),
                    z => InputValueAsserts("other", "String", z)),
                t => FieldDefinitionAsserts("four", "String", t,
                    z =>
                    {
                        InputValueAsserts("argument", "String", z);
                        Assert.IsType<StringValueNode>(z.DefaultValue);
                        Assert.Equal("string", ((StringValueNode)z.DefaultValue).Value);
                    }),
                t => FieldDefinitionAsserts("five", "String", t,
                    z =>
                    {
                        InputValueAsserts("argument", "[String]", z);
                        Assert.IsType<ListValueNode>(z.DefaultValue);
                        Assert.Collection(((ListValueNode)z.DefaultValue).Items,
                            x =>
                            {
                                Assert.IsType<StringValueNode>(x);
                                Assert.Equal("string", ((StringValueNode)x).Value);
                            },
                            x =>
                            {
                                Assert.IsType<StringValueNode>(x);
                                Assert.Equal("string", ((StringValueNode)x).Value);
                            });
                    }),
                t => FieldDefinitionAsserts("six", "Type", t,
                    z =>
                    {
                        InputValueAsserts("argument", "InputType", z);
                        Assert.IsType<ObjectValueNode>(z.DefaultValue);
                        Assert.Collection(((ObjectValueNode)z.DefaultValue).Fields,
                            x =>
                            {
                                Assert.IsType<StringValueNode>(x.Value);
                                Assert.Equal("key", x.Name.Value);
                                Assert.Equal("value", ((StringValueNode)x.Value).Value);
                            });
                    }),
                t => FieldDefinitionAsserts("seven", "Type", t,
                    z =>
                    {
                        InputValueAsserts("argument", "Int", z);
                        Assert.IsType<NullValueNode>(z.DefaultValue);
                    }));
        }

        private void AnnotatedObjectTypeAsserts(IDefinitionNode definitionNode)
        {
            var type = SyntaxNodeAsserts<ObjectTypeDefinitionNode>(
               NodeKind.ObjectTypeDefinition, definitionNode);
            NameAsserts("AnnotatedObject", type.Name);

            Assert.Empty(type.Interfaces);

            Assert.Collection(type.Directives,
                t =>
                {
                    Assert.Equal("onObject", t.Name.Value);
                    Assert.Collection(t.Arguments,
                        x =>
                        {
                            Assert.Equal("arg", x.Name.Value);
                            Assert.Equal("value", ((StringValueNode)x.Value).Value);
                        });
                });

            Assert.Collection(type.Fields,
                t =>
                {
                    FieldDefinitionAsserts("annotatedField", "Type", t,
                        z =>
                        {
                            InputValueAsserts("arg", "Type", z);
                            Assert.IsType<StringValueNode>(z.DefaultValue);
                            Assert.Equal("default", ((StringValueNode)z.DefaultValue).Value);
                            Assert.Collection(z.Directives,
                                y =>
                                {
                                    Assert.Equal("onArg", y.Name.Value);
                                    Assert.Empty(y.Arguments);
                                });
                        });

                    Assert.Collection(t.Directives,
                        z =>
                        {
                            Assert.Equal("onField", z.Name.Value);
                            Assert.Empty(z.Arguments);
                        });
                });
        }

        private void UndefinedTypeTypeAsserts(IDefinitionNode definitionNode)
        {
            var type = SyntaxNodeAsserts<ObjectTypeDefinitionNode>(
               NodeKind.ObjectTypeDefinition, definitionNode);
            NameAsserts("UndefinedType", type.Name);

            Assert.Empty(type.Interfaces);
            Assert.Empty(type.Directives);
            Assert.Empty(type.Fields);
        }

        private void FooTypeExtensionAsserts(IDefinitionNode definitionNode)
        {
            var type = SyntaxNodeAsserts<ObjectTypeExtensionNode>(
               NodeKind.ObjectTypeExtension, definitionNode);

            Assert.Empty(type.Interfaces);
            Assert.Empty(type.Directives);

            Assert.Collection(type.Fields,
                t =>
                {
                    FieldDefinitionAsserts("seven", "Type", t,
                        z =>
                        {
                            InputValueAsserts("argument", "[String]", z);
                            Assert.Empty(z.Directives);
                        });
                    Assert.Empty(t.Directives);
                });
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
            string expectedName, string expectedTypeName,
            FieldDefinitionNode field,
            params Action<InputValueDefinitionNode>[] argumentInsepctors)
        {
            SyntaxNodeAsserts(NodeKind.FieldDefinition, field);
            NameAsserts(expectedName, field.Name);
            TypeAssert(expectedTypeName, field.Type);

            if (argumentInsepctors.Any())
            {
                Assert.Collection(field.Arguments, argumentInsepctors);
            }
            else
            {
                Assert.Empty(field.Arguments);
            }
        }

        private void InputValueAsserts(
            string expectedName, string expectedTypeName,
            InputValueDefinitionNode inputValue)
        {
            NameAsserts(expectedName, inputValue.Name);
            TypeAssert(expectedTypeName, inputValue.Type);
        }

        private void TypeAssert(string expectedName, ITypeNode type)
        {
            string typeName = string.Empty;
            Stack<ITypeNode> stack = new Stack<ITypeNode>();
            ITypeNode current = type;

            while (current != null)
            {
                stack.Push(current);

                if (current is ListTypeNode lt)
                {
                    current = lt.Type;
                }
                else if (current is NonNullTypeNode nt)
                {
                    current = nt.Type;
                }
                else
                {
                    current = null;
                }
            }

            while (stack.Any())
            {
                current = stack.Pop();
                if (current is NamedTypeNode n)
                {
                    typeName = n.Name.Value;
                }
                else if (current is ListTypeNode)
                {
                    typeName = $"[{typeName}]";
                }
                else if (current is NonNullTypeNode)
                {
                    typeName += "!";
                }
            }

            Assert.Equal(expectedName, typeName);
        }

        private T SyntaxNodeAsserts<T>(
            NodeKind expectedNodeKind, ISyntaxNode node)
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