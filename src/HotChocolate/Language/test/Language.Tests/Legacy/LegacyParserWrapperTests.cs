using System;
using System.Linq;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Language
{
    [Obsolete]
    public class LegacyParserWrapperTests
    {
        [Fact]
        public void ParseSimpleShortHandFormQuery()
        {
            // arrange
            string sourceText = "{ x { y } }";

            // act
            var parser = new Parser();
            DocumentNode document = parser.Parse(sourceText);

            // assert
            Assert.Collection(document.Definitions,
                t =>
                {
                    Assert.IsType<OperationDefinitionNode>(t);
                    var operationDefinition = (OperationDefinitionNode)t;
                    Assert.Equal(NodeKind.OperationDefinition, operationDefinition.Kind);
                    Assert.Null(operationDefinition.Name);
                    Assert.Equal(OperationType.Query, operationDefinition.Operation);
                    Assert.Empty(operationDefinition.VariableDefinitions);
                    Assert.Empty(operationDefinition.Directives);

                    Assert.Collection(operationDefinition.SelectionSet.Selections,
                        s1 =>
                        {
                            Assert.IsType<FieldNode>(s1);

                            var field1 = (FieldNode)s1;
                            Assert.Null(field1.Alias);
                            Assert.Equal("x", field1.Name.Value);
                            Assert.Empty(field1.Arguments);
                            Assert.Empty(field1.Directives);

                            Assert.Collection(field1.SelectionSet?.Selections,
                                s2 =>
                                {
                                    Assert.IsType<FieldNode>(s2);

                                    var field2 = (FieldNode)s2;
                                    Assert.Null(field2.Alias);
                                    Assert.Equal("y", field2.Name.Value);
                                    Assert.Empty(field2.Arguments);
                                    Assert.Empty(field2.Directives);
                                    Assert.Null(field2.SelectionSet);
                                });
                        });
                });
        }

        [InlineData("mutation", OperationType.Mutation)]
        [InlineData("query", OperationType.Query)]
        [Theory]
        public void ParserSimpleQuery(string operation, OperationType expectedOperation)
        {
            // arrange
            string sourceText = operation + " a($s : String = \"hello\") { x { y } }";

            // act
            var parser = new Parser();
            DocumentNode document = parser.Parse(sourceText);

            // assert
            Assert.Collection(document.Definitions,
                t =>
                {
                    Assert.IsType<OperationDefinitionNode>(t);
                    var operationDefinition = (OperationDefinitionNode)t;
                    Assert.Equal(NodeKind.OperationDefinition, operationDefinition.Kind);
                    Assert.Equal("a", operationDefinition.Name?.Value);
                    Assert.Equal(expectedOperation, operationDefinition.Operation);
                    Assert.Empty(operationDefinition.Directives);

                    Assert.Collection(operationDefinition.VariableDefinitions,
                        v1 =>
                        {
                            Assert.Equal("s", v1.Variable.Name.Value);
                            Assert.Equal("String", ((NamedTypeNode)v1.Type).Name.Value);
                            Assert.IsType<StringValueNode>(v1.DefaultValue);
                        });

                    Assert.Collection(operationDefinition.SelectionSet.Selections,
                        s1 =>
                        {
                            Assert.IsType<FieldNode>(s1);

                            var field1 = (FieldNode)s1;
                            Assert.Null(field1.Alias);
                            Assert.Equal("x", field1.Name.Value);
                            Assert.Empty(field1.Arguments);
                            Assert.Empty(field1.Directives);

                            Assert.Collection(field1.SelectionSet?.Selections,
                                s2 =>
                                {
                                    Assert.IsType<FieldNode>(s2);

                                    var field2 = (FieldNode)s2;
                                    Assert.Null(field2.Alias);
                                    Assert.Equal("y", field2.Name.Value);
                                    Assert.Empty(field2.Arguments);
                                    Assert.Empty(field2.Directives);
                                    Assert.Null(field2.SelectionSet);
                                });
                        });
                });
        }

        [Fact]
        public void ParseQueryWithFragment()
        {
            // arrange
            string sourceText = "query a { x { ... y } } fragment y on Type { z } ";
            var source = new Source(sourceText);

            // act
            var parser = new Parser();
            DocumentNode document = parser.Parse(sourceText);

            // assert
            Assert.Collection(document.Definitions,
                t =>
                {
                    Assert.IsType<OperationDefinitionNode>(t);
                    var operationDefinition = (OperationDefinitionNode)t;
                    Assert.Equal(NodeKind.OperationDefinition, operationDefinition.Kind);
                    Assert.Equal("a", operationDefinition.Name?.Value);
                    Assert.Equal(OperationType.Query, operationDefinition.Operation);
                    Assert.Empty(operationDefinition.VariableDefinitions);
                    Assert.Empty(operationDefinition.Directives);

                    Assert.Collection(operationDefinition.SelectionSet.Selections,
                        s1 =>
                        {
                            Assert.IsType<FieldNode>(s1);

                            var field1 = (FieldNode)s1;
                            Assert.Null(field1.Alias);
                            Assert.Equal("x", field1.Name.Value);
                            Assert.Empty(field1.Arguments);
                            Assert.Empty(field1.Directives);

                            Assert.Collection(field1.SelectionSet?.Selections,
                                s2 =>
                                {
                                    Assert.IsType<FragmentSpreadNode>(s2);

                                    var spread = (FragmentSpreadNode)s2;
                                    Assert.Equal("y", spread.Name.Value);
                                    Assert.Empty(spread.Directives);
                                });
                        });
                },
                t =>
                {
                    Assert.IsType<FragmentDefinitionNode>(t);
                    var fragmentDefinition = (FragmentDefinitionNode)t;
                    Assert.Equal("y", fragmentDefinition.Name.Value);
                    Assert.Equal("Type", fragmentDefinition.TypeCondition.Name.Value);
                    Assert.Empty(fragmentDefinition.VariableDefinitions);
                    Assert.Empty(fragmentDefinition.Directives);

                    ISelectionNode selectionNode = fragmentDefinition
                        .SelectionSet.Selections.Single();
                    Assert.IsType<FieldNode>(selectionNode);
                    Assert.Equal("z", ((FieldNode)selectionNode).Name.Value);

                });
        }

        [Fact]
        public void QueryWithComments()
        {
            // arrange
            string query = @"{
                hero {
                    name
                    # Queries can have comments!
                    friends {
                        name
                    }
                }
            }";

            // act
            DocumentNode document = Utf8GraphQLParser.Parse(query,
                new ParserOptions(noLocations: true));

            // assert
            document.MatchSnapshot();
        }

        [Fact]
        public void ParseDirectiveOnVariableDefinition()
        {
            // arrange
            string sourceText =
                "query queryName($foo: ComplexType @foo) { bar }";

            // act
            DocumentNode document = Utf8GraphQLParser.Parse(sourceText);

            // assert
            Assert.Collection(
                document.Definitions.OfType<OperationDefinitionNode>().First()
                    .VariableDefinitions.First()
                    .Directives,
                d => Assert.Equal("foo", d.Name.Value));
        }
    }
}
