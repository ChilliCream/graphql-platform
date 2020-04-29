using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Types;
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
            IReadOnlyList<PreparedSelectionSet> selectionSets = 
                FieldCollector.PrepareSelectionSets(schema, fragments, operation);

            // assert
            Assert.Collection(
                selectionSets,
                selectionSet => 
                {
                    Assert.Equal(schema.QueryType, selectionSet.TypeContext);
                    Assert.Equal(operation.SelectionSet, selectionSet.SelectionSet);
                    Assert.Collection(
                        selectionSet.Selections,
                        selection => Assert.Equal("foo",  selection.ResponseName));
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
            IReadOnlyList<PreparedSelectionSet> selectionSets = 
                FieldCollector.PrepareSelectionSets(schema, fragments, operation);

            // assert
            Assert.Collection(
                selectionSets,
                selectionSet => 
                {
                    Assert.Equal(schema.QueryType, selectionSet.TypeContext);
                    Assert.Equal(operation.SelectionSet, selectionSet.SelectionSet);
                    Assert.Collection(
                        selectionSet.Selections,
                        selection => Assert.Equal("foo",  selection.ResponseName));
                });
        }
    }
}
