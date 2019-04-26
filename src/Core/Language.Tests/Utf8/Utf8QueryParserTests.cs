﻿using System.Linq;
using System.Text;
using ChilliCream.Testing;
using Snapshooter;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Language
{
    public class Utf8QueryParserTests
    {
        [Fact]
        public void ParseSimpleShortHandFormQuery()
        {
            // arrange
            byte[] sourceText = Encoding.UTF8.GetBytes("{ x { y } }");

            // act
            var parser = new Utf8GraphQLParser(
                sourceText, ParserOptions.Default);
            DocumentNode document = parser.Parse();

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

                            FieldNode field1 = (FieldNode)s1;
                            Assert.Null(field1.Alias);
                            Assert.Equal("x", field1.Name.Value);
                            Assert.Empty(field1.Arguments);
                            Assert.Empty(field1.Directives);

                            Assert.Collection(field1.SelectionSet.Selections,
                                s2 =>
                                {
                                    Assert.IsType<FieldNode>(s2);

                                    FieldNode field2 = (FieldNode)s2;
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
        [InlineData("subscription", OperationType.Subscription)]
        [Theory]
        public void ParserSimpleQuery(string operation, OperationType expectedOperation)
        {
            // arrange
            byte[] sourceText = Encoding.UTF8.GetBytes(
                operation + " a($s : String = \"hello\") { x { y } }");

            // act
            var parser = new Utf8GraphQLParser(
                sourceText, ParserOptions.Default);
            DocumentNode document = parser.Parse();

            // assert
            Assert.Collection(document.Definitions,
                t =>
                {
                    Assert.IsType<OperationDefinitionNode>(t);
                    var operationDefinition = (OperationDefinitionNode)t;
                    Assert.Equal(NodeKind.OperationDefinition, operationDefinition.Kind);
                    Assert.Equal("a", operationDefinition.Name.Value);
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

                            FieldNode field1 = (FieldNode)s1;
                            Assert.Null(field1.Alias);
                            Assert.Equal("x", field1.Name.Value);
                            Assert.Empty(field1.Arguments);
                            Assert.Empty(field1.Directives);

                            Assert.Collection(field1.SelectionSet.Selections,
                                s2 =>
                                {
                                    Assert.IsType<FieldNode>(s2);

                                    FieldNode field2 = (FieldNode)s2;
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
            byte[] sourceText = Encoding.UTF8.GetBytes(
                "query a { x { ... y } } fragment y on Type { z } ");

            // act
            var parser = new Utf8GraphQLParser(
                sourceText, ParserOptions.Default);
            DocumentNode document = parser.Parse();

            // assert
            Assert.Collection(document.Definitions,
                t =>
                {
                    Assert.IsType<OperationDefinitionNode>(t);
                    var operationDefinition = (OperationDefinitionNode)t;
                    Assert.Equal(NodeKind.OperationDefinition, operationDefinition.Kind);
                    Assert.Equal("a", operationDefinition.Name.Value);
                    Assert.Equal(OperationType.Query, operationDefinition.Operation);
                    Assert.Empty(operationDefinition.VariableDefinitions);
                    Assert.Empty(operationDefinition.Directives);

                    Assert.Collection(operationDefinition.SelectionSet.Selections,
                        s1 =>
                        {
                            Assert.IsType<FieldNode>(s1);

                            FieldNode field1 = (FieldNode)s1;
                            Assert.Null(field1.Alias);
                            Assert.Equal("x", field1.Name.Value);
                            Assert.Empty(field1.Arguments);
                            Assert.Empty(field1.Directives);

                            Assert.Collection(field1.SelectionSet.Selections,
                                s2 =>
                                {
                                    Assert.IsType<FragmentSpreadNode>(s2);

                                    FragmentSpreadNode spread = (FragmentSpreadNode)s2;
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
            byte[] sourceText = Encoding.UTF8.GetBytes(
                @"{
                hero {
                    name
                    # Queries can have comments!
                    friends(a:""foo"" b: 123456 c:null d:     true) {
                        name
                    }
                }
            }");

            // act
            var parser = new Utf8GraphQLParser(
                sourceText, ParserOptions.Default);
            DocumentNode document = parser.Parse();

            // assert
            document.MatchSnapshot();
            QuerySyntaxSerializer.Serialize(document)
                .MatchSnapshot(new SnapshotNameExtension("serialized"));
        }

        [Fact]
        public void IntrospectionQuery()
        {
            // arrange
            byte[] sourceText = Encoding.UTF8.GetBytes(
                FileResource.Open("IntrospectionQuery.graphql"));

            // act
            var parser = new Utf8GraphQLParser(
                sourceText, ParserOptions.Default);
            DocumentNode document = parser.Parse();

            // assert
            document.MatchSnapshot();
        }

        [Fact]
        public void KitchenSinkQueryQuery()
        {
            // arrange
            byte[] sourceText = Encoding.UTF8.GetBytes(
                FileResource.Open("kitchen-sink.graphql"));

            // act
            var parser = new Utf8GraphQLParser(
                sourceText, ParserOptions.Default);
            DocumentNode document = parser.Parse();

            // assert
            document.MatchSnapshot();
        }

        [Fact]
        public void QueryWithStringArg()
        {
            // arrange
            byte[] sourceText = Encoding.UTF8.GetBytes(
                FileResource.Open("QueryWithStringArg.graphql"));

            // act
            var parser = new Utf8GraphQLParser(
                sourceText, ParserOptions.Default);
            DocumentNode document = parser.Parse();

            // assert
            document.MatchSnapshot();
        }


        [Fact]
        public void StringArg()
        {
            // arrange
            byte[] sourceText = Encoding.UTF8.GetBytes(
                "{ a(b:\"Q3VzdG9tZXIteDE=\") }");

            // act
            var parser = new Utf8GraphQLParser(
                sourceText, ParserOptions.Default);
            DocumentNode document = parser.Parse();

            // assert
            StringValueNode value = Assert.IsType<StringValueNode>(
                document.Definitions.OfType<OperationDefinitionNode>().First()
                .SelectionSet.Selections.OfType<FieldNode>().First()
                .Arguments.First().Value);

            Assert.Equal("Q3VzdG9tZXIteDE=", value.Value);
        }

        [InlineData("1234")]
        [InlineData("-1234")]
        [Theory]
        public void IntArg(string arg)
        {
            // arrange
            byte[] sourceText = Encoding.UTF8.GetBytes(
                "{ a(b:" + arg + ") }");

            // act
            var parser = new Utf8GraphQLParser(
                sourceText, ParserOptions.Default);
            DocumentNode document = parser.Parse();

            // assert
            IntValueNode value = Assert.IsType<IntValueNode>(
                document.Definitions.OfType<OperationDefinitionNode>().First()
                .SelectionSet.Selections.OfType<FieldNode>().First()
                .Arguments.First().Value);

            Assert.Equal(arg, value.Value);
        }

        [InlineData("1234.123")]
        [InlineData("-1234.123")]
        [InlineData("1e50")]
        [InlineData("6.0221413e23")]
        [Theory]
        public void FloatArg(string arg)
        {
            // arrange
            byte[] sourceText = Encoding.UTF8.GetBytes(
                "{ a(b:" + arg + ") }");

            // act
            var parser = new Utf8GraphQLParser(
                sourceText, ParserOptions.Default);
            DocumentNode document = parser.Parse();

            // assert
            FloatValueNode value = Assert.IsType<FloatValueNode>(
                document.Definitions.OfType<OperationDefinitionNode>().First()
                .SelectionSet.Selections.OfType<FieldNode>().First()
                .Arguments.First().Value);

            Assert.Equal(arg, value.Value);
        }

        [InlineData("true", true)]
        [InlineData("false", false)]
        [Theory]
        public void BooleanArg(string arg, bool expected)
        {
            // arrange
            byte[] sourceText = Encoding.UTF8.GetBytes(
                "{ a(b:" + arg + ") }");

            // act
            var parser = new Utf8GraphQLParser(
                sourceText, ParserOptions.Default);
            DocumentNode document = parser.Parse();

            // assert
            BooleanValueNode value = Assert.IsType<BooleanValueNode>(
                document.Definitions.OfType<OperationDefinitionNode>().First()
                .SelectionSet.Selections.OfType<FieldNode>().First()
                .Arguments.First().Value);

            Assert.Equal(expected, value.Value);
        }

        [InlineData("ABC")]
        [InlineData("DEF")]
        [Theory]
        public void EnumArg(string arg)
        {
            // arrange
            byte[] sourceText = Encoding.UTF8.GetBytes(
                "{ a(b:" + arg + ") }");

            // acts
            var parser = new Utf8GraphQLParser(
                sourceText, ParserOptions.Default);
            DocumentNode document = parser.Parse();

            // assert
            EnumValueNode value = Assert.IsType<EnumValueNode>(
                document.Definitions.OfType<OperationDefinitionNode>().First()
                .SelectionSet.Selections.OfType<FieldNode>().First()
                .Arguments.First().Value);

            Assert.Equal(arg, value.Value);
        }

        [Fact]
        public void NullArg()
        {
            // arrange
            byte[] sourceText = Encoding.UTF8.GetBytes(
                "{ a(b:null) }");

            // acts
            var parser = new Utf8GraphQLParser(
                sourceText, ParserOptions.Default);
            DocumentNode document = parser.Parse();

            // assert
            Assert.IsType<NullValueNode>(
                document.Definitions.OfType<OperationDefinitionNode>().First()
                .SelectionSet.Selections.OfType<FieldNode>().First()
                .Arguments.First().Value);
        }
    }
}


