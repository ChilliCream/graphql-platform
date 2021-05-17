using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.StarWars;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Execution.Processing.Plan
{
    public class QueryPlanBuilderTests
    {
        [Fact]
        public void GetHero_Plan()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddStarWarsTypes()
                .Create();

            DocumentNode document = Utf8GraphQLParser.Parse(
                @"query GetHero($episode: Episode, $withFriends: Boolean!) {
                    hero(episode: $episode) {
                        name
                        friends @include(if: $withFriends) {
                            nodes {
                                id
                            }
                        }
                    }
                }");

            OperationDefinitionNode operationDefinition =
                document.Definitions.OfType<OperationDefinitionNode>().Single();

            IPreparedOperation operation =
                OperationCompiler.Compile(
                    "a",
                    document,
                    operationDefinition,
                    schema,
                    schema.QueryType);

            // act
            QueryPlanNode root = QueryPlanBuilder.Prepare(operation);

            // assert
            Snapshot(root);
        }

        [Fact]
        public void CreateReviewForEpisode_Plan()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddStarWarsTypes()
                .Create();

            DocumentNode document = Utf8GraphQLParser.Parse(
                @"mutation CreateReviewForEpisode(
                    $ep: Episode!, $review: ReviewInput!) {
                    createReview(episode: $ep, review: $review) {
                        stars
                        commentary
                    }
                }");

            OperationDefinitionNode operationDefinition =
                document.Definitions.OfType<OperationDefinitionNode>().Single();

            IPreparedOperation operation =
                OperationCompiler.Compile(
                    "a",
                    document,
                    operationDefinition,
                    schema,
                    schema.MutationType);

            // act
            QueryPlanNode root = QueryPlanBuilder.Prepare(operation);

            // assert
            Snapshot(root);
        }

        private static void Snapshot(QueryPlanNode node)
        {
            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true});
            node.Serialize(writer);
            writer.Flush();
            Encoding.UTF8.GetString(stream.ToArray()).MatchSnapshot();
        }
    }
}
