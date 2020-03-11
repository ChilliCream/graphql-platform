
using System.Linq;
using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;
using StrawberryShake.CodeGeneration.Utilities;
using Xunit;

namespace StrawberryShake.CodeGeneration.Analyzers
{
    public class InterfaceTypeSelectionSetAnalyzerTests
    {
        [Fact]
        public void Interface_Two_Types_One_Inline_Fragment_Per_Type()
        {
            // arrange
            ISchema schema =
                SchemaBuilder.New()
                    .Use(next => context => Task.CompletedTask)
                    .AddDocumentFromString(@"
                    type Query {
                      foo: Foo
                    }

                    interface Foo {
                      id: String
                      name: String
                    }

                    type Bar implements Foo {
                      id: String
                      name: String
                      bar: String
                    }

                    type Baz implements Foo {
                      id: String
                      name: String
                      baz: String
                    }")
                    .Create();

            DocumentNode document =
                Utf8GraphQLParser.Parse(@"
                {
                  foo {
                    id
                    name
                    ... on Bar {
                      bar
                    }
                    ... on Baz {
                      baz
                    }
                  }
                }");

            OperationDefinitionNode operation =
                document.Definitions.OfType<OperationDefinitionNode>().Single();

            FieldNode field =
                operation.SelectionSet.Selections.OfType<FieldNode>().Single();

            var fragmentCollection = new FragmentCollection(schema, document);
            var fieldCollector = new FieldCollector(schema, fragmentCollection);
            var context = new DocumentAnalyzerContext(schema);
            context.SetDocument(fieldCollector);

            InterfaceType fooType = schema.GetType<InterfaceType>("Foo");
            Path rootPath = Path.New("foo");

            PossibleSelections possibleSelections =
                fieldCollector.CollectFields(
                    fooType,
                    field.SelectionSet,
                    rootPath);

            // act
            var analyzer = new InterfaceTypeSelectionSetAnalyzer();
            analyzer.Analyze(
                context,
                operation,
                field,
                possibleSelections,
                schema.QueryType.Fields["foo"].Type,
                fooType,
                rootPath);

            // assert

        }

        [Fact]
        public void Interface_With_2_Selections_Per_Type()
        {
            // arrange
            ISchema schema =
                SchemaBuilder.New()
                    .Use(next => context => Task.CompletedTask)
                    .AddDocumentFromString(@"
                    type Query {
                      foo: Foo
                    }

                    interface Foo {
                      id: String
                      name: String
                    }

                    type Bar implements Foo {
                      id: String
                      name: String
                      bar: String
                      bar2: String
                    }

                    type Baz implements Foo {
                      id: String
                      name: String
                      baz: String
                      baz2: String
                    }")
                    .Create();

            DocumentNode document =
                Utf8GraphQLParser.Parse(@"
                {
                  foo {
                    id
                    name
                    ... on Bar {
                      bar
                    }
                    ... on Bar {
                      bar2
                    }
                    ... on Baz {
                      baz
                    }
                    ... on Baz {
                      baz2
                    }
                  }
                }");

            OperationDefinitionNode operation =
                document.Definitions.OfType<OperationDefinitionNode>().Single();

            FieldNode field =
                operation.SelectionSet.Selections.OfType<FieldNode>().Single();

            var fragmentCollection = new FragmentCollection(schema, document);
            var fieldCollector = new FieldCollector(schema, fragmentCollection);
            var context = new DocumentAnalyzerContext(schema);
            context.SetDocument(fieldCollector);

            InterfaceType fooType = schema.GetType<InterfaceType>("Foo");
            Path rootPath = Path.New("foo");

            PossibleSelections possibleSelections =
                fieldCollector.CollectFields(
                    fooType,
                    field.SelectionSet,
                    rootPath);

            // act
            var analyzer = new InterfaceTypeSelectionSetAnalyzer();
            analyzer.Analyze(
                context,
                operation,
                field,
                possibleSelections,
                schema.QueryType.Fields["foo"].Type,
                fooType,
                rootPath);

            // assert
            /*
            Assert.Collection(context.Types.OfType<ComplexOutputTypeModel>(),
                type =>
                {
                    Assert.Equal("Foo", type.Name);
                    Assert.Null(type.Description);
                    Assert.Equal(fooType, type.Type);
                    Assert.Equal(field.SelectionSet, type.SelectionSet);
                    Assert.Empty(type.Types);
                    Assert.Collection(type.Fields,
                        field =>
                        {
                            Assert.Equal("Bar", field.Name);
                            Assert.Null(field.Description);
                            Assert.Equal(fooType.Fields["bar"], field.Field);
                            Assert.Equal(fooType.Fields["bar"].Type, field.Type);
                            Assert.Equal(rootPath.Append("bar"), field.Path);
                        });
                });
                */
        }

         [Fact]
        public void Interface_With_2_Selections_No_Shared_Fields()
        {
            // arrange
            ISchema schema =
                SchemaBuilder.New()
                    .Use(next => context => Task.CompletedTask)
                    .AddDocumentFromString(@"
                    type Query {
                      foo: Foo
                    }

                    interface Foo {
                      id: String
                      name: String
                    }

                    type Bar implements Foo {
                      id: String
                      name: String
                      bar: String
                    }

                    type Baz implements Foo {
                      id: String
                      name: String
                      baz: String
                    }")
                    .Create();

            DocumentNode document =
                Utf8GraphQLParser.Parse(@"
                {
                  foo {
                    ... on Bar {
                      id
                      name
                      bar
                    }
                    ... on Baz {
                      id
                      name
                      baz
                    }
                  }
                }");

            OperationDefinitionNode operation =
                document.Definitions.OfType<OperationDefinitionNode>().Single();

            FieldNode field =
                operation.SelectionSet.Selections.OfType<FieldNode>().Single();

            var fragmentCollection = new FragmentCollection(schema, document);
            var fieldCollector = new FieldCollector(schema, fragmentCollection);
            var context = new DocumentAnalyzerContext(schema);
            context.SetDocument(fieldCollector);

            InterfaceType fooType = schema.GetType<InterfaceType>("Foo");
            Path rootPath = Path.New("foo");

            PossibleSelections possibleSelections =
                fieldCollector.CollectFields(
                    fooType,
                    field.SelectionSet,
                    rootPath);

            // act
            var analyzer = new InterfaceTypeSelectionSetAnalyzer();
            analyzer.Analyze(
                context,
                operation,
                field,
                possibleSelections,
                schema.QueryType.Fields["foo"].Type,
                fooType,
                rootPath);

            // assert
            /*
            Assert.Collection(context.Types.OfType<ComplexOutputTypeModel>(),
                type =>
                {
                    Assert.Equal("Foo", type.Name);
                    Assert.Null(type.Description);
                    Assert.Equal(fooType, type.Type);
                    Assert.Equal(field.SelectionSet, type.SelectionSet);
                    Assert.Empty(type.Types);
                    Assert.Collection(type.Fields,
                        field =>
                        {
                            Assert.Equal("Bar", field.Name);
                            Assert.Null(field.Description);
                            Assert.Equal(fooType.Fields["bar"], field.Field);
                            Assert.Equal(fooType.Fields["bar"].Type, field.Type);
                            Assert.Equal(rootPath.Append("bar"), field.Path);
                        });
                });
                */
        }
    }
}
