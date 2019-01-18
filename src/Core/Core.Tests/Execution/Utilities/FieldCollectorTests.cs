using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Utilities;
using Xunit;

namespace HotChocolate.Execution
{
    public class FieldCollectorTests
    {
        [Fact]
        public void MergeFieldsWithFragmentSpreads()
        {
            // arrange
            DocumentNode query = Parser.Default.Parse(
                FileResource.Open("MergeQuery.graphql"));

            OperationDefinitionNode operation =
                query.Definitions.OfType<OperationDefinitionNode>().Single();

            Schema schema = Schema.Create(
                FileResource.Open("MergeSchema.graphql"),
                c => c.Use(next => context => Task.CompletedTask));

            var fragments = new FragmentCollection(schema, query);

            var variables = new VariableCollection(
                TypeConversion.Default,
                new Dictionary<string, object>());

            // act
            var collector = new FieldCollector(variables, fragments);
            IReadOnlyCollection<FieldSelection> selections =
                collector.CollectFields(schema.QueryType,
                    operation.SelectionSet, error => { });

            // assert
            Assert.Collection(selections,
                selection =>
                {
                    Assert.Collection(selection.Selection.SelectionSet.Selections,
                        selectionNode =>
                        {
                            var fragment = Assert.IsType<FragmentSpreadNode>(
                                selectionNode);
                            Assert.Equal("app", fragment.Name.Value);
                        },
                        selectionNode =>
                        {
                            var fragment = Assert.IsType<FragmentSpreadNode>(
                                selectionNode);
                            Assert.Equal("parts", fragment.Name.Value);
                        });
                });
        }

        [Fact]
        public void MergeMerged()
        {
            // arrange
            DocumentNode query = Parser.Default.Parse(
                FileResource.Open("MergeQuery.graphql"));

            OperationDefinitionNode operation =
                query.Definitions.OfType<OperationDefinitionNode>().Single();

            Schema schema = Schema.Create(
                FileResource.Open("MergeSchema.graphql"),
                c => c.Use(next => context => Task.CompletedTask));

            var fragments = new FragmentCollection(schema, query);

            var variables = new VariableCollection(
                TypeConversion.Default,
                new Dictionary<string, object>());

            var collector = new FieldCollector(variables, fragments);

            IReadOnlyCollection<FieldSelection> selections =
                collector.CollectFields(schema.QueryType,
                    operation.SelectionSet, error => { });

            // act
            selections = collector.CollectFields(
                schema.GetType<ObjectType>("Application"),
                selections.Single().Selection.SelectionSet,
                error => { });

            // assert
            Assert.Collection(selections,
                selection => Assert.Equal("id", selection.ResponseName),
                selection => Assert.Equal("name", selection.ResponseName),
                selection => Assert.Equal("parts", selection.ResponseName));
        }
    }
}
