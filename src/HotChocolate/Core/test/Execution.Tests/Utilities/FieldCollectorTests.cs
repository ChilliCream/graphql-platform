using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.StarWars;
using HotChocolate.Types;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Execution.Utilities
{
    public class FieldCollectorTests
    {
        [Fact]
        public void Prepare_One_Field()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(c => c
                    .Name("Query")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolver("foo"))
                .Create();

            DocumentNode document = Utf8GraphQLParser.Parse("{ foo }");

            OperationDefinitionNode operation =
                document.Definitions.OfType<OperationDefinitionNode>().Single();

            var fragments = new FragmentCollection(schema, document);

            // act
            IReadOnlyDictionary<SelectionSetNode, PreparedSelectionSet> selectionSets =
                FieldCollector.PrepareSelectionSets(schema, fragments, operation);

            // assert
            Assert.Collection(
                selectionSets.Values,
                selectionSet =>
                {
                    Assert.Equal(operation.SelectionSet, selectionSet.SelectionSet);
                    Assert.Collection(
                        selectionSet.GetSelections(schema.QueryType),
                        selection => Assert.Equal("foo", selection.ResponseName));
                });
        }

        [Fact]
        public void Prepare_Duplicate_Field()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(c => c
                    .Name("Query")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolver("foo"))
                .Create();

            DocumentNode document = Utf8GraphQLParser.Parse("{ foo foo }");

            OperationDefinitionNode operation =
                document.Definitions.OfType<OperationDefinitionNode>().Single();

            var fragments = new FragmentCollection(schema, document);

            // act
            IReadOnlyDictionary<SelectionSetNode, PreparedSelectionSet> selectionSets =
                FieldCollector.PrepareSelectionSets(schema, fragments, operation);

            // assert
            Assert.Collection(
                selectionSets.Values,
                selectionSet =>
                {
                    Assert.Equal(operation.SelectionSet, selectionSet.SelectionSet);
                    Assert.Collection(
                        selectionSet.GetSelections(schema.QueryType),
                        selection => Assert.Equal("foo", selection.ResponseName));
                });
        }

        [Fact]
        public void Prepare_Inline_Fragment()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddStarWarsTypes()
                .Create();

            DocumentNode document = Utf8GraphQLParser.Parse(
            @"{
                hero(episode: EMPIRE) {
                    name
                    ... on Droid {
                        primaryFunction
                    }
                    ... on Human {
                        homePlanet
                    }
                }
             }");

            OperationDefinitionNode operation =
                document.Definitions.OfType<OperationDefinitionNode>().Single();

            var fragments = new FragmentCollection(schema, document);

            // act
            IReadOnlyDictionary<SelectionSetNode, PreparedSelectionSet> selectionSets =
                FieldCollector.PrepareSelectionSets(schema, fragments, operation);

            // assert
            IPreparedSelection hero = selectionSets[operation.SelectionSet].GetSelections(schema.QueryType).Single();
            Assert.Equal("hero", hero.ResponseName);

            Assert.Collection(
                selectionSets[hero.SelectionSet].GetSelections(schema.GetType<ObjectType>("Droid")),
                selection => Assert.Equal("name", selection.ResponseName),
                selection => Assert.Equal("primaryFunction", selection.ResponseName));

            Assert.Collection(
                selectionSets[hero.SelectionSet].GetSelections(schema.GetType<ObjectType>("Human")),
                selection => Assert.Equal("name", selection.ResponseName),
                selection => Assert.Equal("homePlanet", selection.ResponseName));

            var op = new PreparedOperation(
                "abc",
                document,
                operation,
                schema.QueryType,
                selectionSets);
            op.Print().MatchSnapshot();
        }

        [Fact]
        public void Prepare_Fragment_Definition()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddStarWarsTypes()
                .Create();

            DocumentNode document = Utf8GraphQLParser.Parse(
            @"{
                hero(episode: EMPIRE) {
                    name
                    ... abc
                    ... def
                }
              }

              fragment abc on Droid {
                  primaryFunction
              }

              fragment def on Human {
                  homePlanet
              }
             ");

            OperationDefinitionNode operation =
                document.Definitions.OfType<OperationDefinitionNode>().Single();

            var fragments = new FragmentCollection(schema, document);

            // act
            IReadOnlyDictionary<SelectionSetNode, PreparedSelectionSet> selectionSets =
                FieldCollector.PrepareSelectionSets(schema, fragments, operation);

            // assert
            var op = new PreparedOperation(
                "abc",
                document,
                operation,
                schema.QueryType,
                selectionSets);
            op.Print().MatchSnapshot();
        }

        [Fact]
        public void Prepare_Duplicate_Field_With_Skip()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(c => c
                    .Name("Query")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolver("foo"))
                .Create();

            DocumentNode document = Utf8GraphQLParser.Parse("{ foo @skip(if: true) foo @skip(if: false) }");

            OperationDefinitionNode operation =
                document.Definitions.OfType<OperationDefinitionNode>().Single();

            var fragments = new FragmentCollection(schema, document);

            // act
            IReadOnlyDictionary<SelectionSetNode, PreparedSelectionSet> selectionSets =
                FieldCollector.PrepareSelectionSets(schema, fragments, operation);

            // assert
            Assert.Collection(
                selectionSets.Values,
                selectionSet =>
                {
                    Assert.Equal(operation.SelectionSet, selectionSet.SelectionSet);
                    Assert.Collection(
                        selectionSet.GetSelections(schema.QueryType),
                        selection => Assert.Equal("foo", selection.ResponseName));
                });
        }
    }
}
