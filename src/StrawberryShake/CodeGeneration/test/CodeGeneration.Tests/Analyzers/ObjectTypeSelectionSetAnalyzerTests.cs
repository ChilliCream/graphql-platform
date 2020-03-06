
using System.Linq;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using StrawberryShake.CodeGeneration.Utilities;
using Xunit;

namespace StrawberryShake.CodeGeneration.Analyzers
{
    public class ObjectTypeSelectionSetAnalyzerTests
    {
        [Fact]
        public void Simple_Object_Selection()
        {
            // arrange
            ISchema schema =
                SchemaBuilder.New()
                    .AddQueryType<Query>()
                    .Create();

            DocumentNode document =
                Utf8GraphQLParser.Parse("{ foo { bar { baz } } }");

            OperationDefinitionNode operation =
                document.Definitions.OfType<OperationDefinitionNode>().Single();

            FieldNode field =
                operation.SelectionSet.Selections.OfType<FieldNode>().Single();

            var fragmentCollection = new FragmentCollection(schema, document);
            var fieldCollector = new FieldCollector(schema, fragmentCollection);
            var context = new DocumentAnalyzerContext(schema, fieldCollector);

            ObjectType fooType = schema.GetType<ObjectType>("Foo");
            Path rootPath = Path.New("foo");

            PossibleSelections possibleSelections =
                fieldCollector.CollectFields(
                    fooType,
                    field.SelectionSet,
                    rootPath);

            // act
            var analyzer = new ObjectTypeSelectionSetAnalyzer();
            analyzer.Analyze(
                context,
                operation,
                field,
                possibleSelections,
                schema.QueryType.Fields["foo"].Type,
                fooType,
                rootPath);

            // assert
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
        }

        [Fact]
        public void Simple_Object_Selection_With_Alias()
        {
            // arrange
            ISchema schema =
                SchemaBuilder.New()
                    .AddQueryType<Query>()
                    .Create();

            DocumentNode document =
                Utf8GraphQLParser.Parse("{ foo { b: bar { baz } } }");

            OperationDefinitionNode operation =
                document.Definitions.OfType<OperationDefinitionNode>().Single();

            FieldNode field =
                operation.SelectionSet.Selections.OfType<FieldNode>().Single();

            var fragmentCollection = new FragmentCollection(schema, document);
            var fieldCollector = new FieldCollector(schema, fragmentCollection);
            var context = new DocumentAnalyzerContext(schema, fieldCollector);

            ObjectType fooType = schema.GetType<ObjectType>("Foo");
            Path rootPath = Path.New("foo");

            PossibleSelections possibleSelections =
                fieldCollector.CollectFields(
                    fooType,
                    field.SelectionSet,
                    rootPath);

            // act
            var analyzer = new ObjectTypeSelectionSetAnalyzer();
            analyzer.Analyze(
                context,
                operation,
                field,
                possibleSelections,
                schema.QueryType.Fields["foo"].Type,
                fooType,
                rootPath);

            // assert
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
                            Assert.Equal("B", field.Name);
                            Assert.Null(field.Description);
                            Assert.Equal(fooType.Fields["bar"], field.Field);
                            Assert.Equal(fooType.Fields["bar"].Type, field.Type);
                            Assert.Equal(rootPath.Append("b"), field.Path);
                        });
                });
        }

        public class Query
        {
            public Foo Foo => new Foo();
        }

        public class Foo
        {
            public Bar Bar => new Bar();
        }

        public class Bar
        {
            public string Baz => "Baz";
        }
    }
}
