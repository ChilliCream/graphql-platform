using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.StarWars;
using HotChocolate.Types;
using Snapshooter;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Execution.Processing.Plan
{
    public class QueryPlanBuilderTests
    {
        [InlineData(ExecutionStrategy.Parallel)]
        [InlineData(ExecutionStrategy.Serial)]
        [Theory]
        public void GetHero_Plan(ExecutionStrategy defaultStrategy)
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddStarWarsTypes()
                .ModifyOptions(o => o.DefaultResolverStrategy = defaultStrategy)
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
            Snapshot(root, defaultStrategy);
        }

        [InlineData(ExecutionStrategy.Parallel)]
        [InlineData(ExecutionStrategy.Serial)]
        [Theory]
        public void GetHero_Root_Deferred_Plan(ExecutionStrategy defaultStrategy)
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddStarWarsTypes()
                .ModifyOptions(o => o.DefaultResolverStrategy = defaultStrategy)
                .Create();

            DocumentNode document = Utf8GraphQLParser.Parse(
                @"query GetHero($episode: Episode, $withFriends: Boolean!) {
                    ... @defer {
                        hero(episode: $episode) {
                            name
                            friends @include(if: $withFriends) {
                                nodes {
                                    id
                                }
                            }
                        }
                    }
                    a: hero(episode: $episode) {
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
            Snapshot(root, defaultStrategy);
        }

        [InlineData(ExecutionStrategy.Parallel)]
        [InlineData(ExecutionStrategy.Serial)]
        [Theory]
        public void CreateReviewForEpisode_Plan(ExecutionStrategy defaultStrategy)
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddStarWarsTypes()
                .ModifyOptions(o => o.DefaultResolverStrategy = defaultStrategy)
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
            Snapshot(root, defaultStrategy);
        }

        [InlineData(ExecutionStrategy.Parallel)]
        [InlineData(ExecutionStrategy.Serial)]
        [Theory]
        public void CreateTwoReviewsForEpisode_Plan(ExecutionStrategy defaultStrategy)
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddStarWarsTypes()
                .ModifyOptions(o => o.DefaultResolverStrategy = defaultStrategy)
                .Create();

            DocumentNode document = Utf8GraphQLParser.Parse(
                @"mutation CreateReviewForEpisode(
                    $ep: Episode!, $ep2: Episode!, $review: ReviewInput!) {
                    createReview(episode: $ep, review: $review) {
                        stars
                        commentary
                    }
                    b: createReview(episode: $ep2, review: $review) {
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
            Snapshot(root, defaultStrategy);
        }

        [Fact]
        public void TypeNameFieldsInMutations()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString(@"
                    type Query {
                        foo: String
                    }

                    type Mutation {
                        bar: Bar
                    }

                    type Bar {
                        test: String
                    }
                ")
                .Use(next => next)
                .Create();

            DocumentNode document = Utf8GraphQLParser.Parse(
                @"mutation {
                    bar {
                        test
                        __typename
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

        [InlineData(ExecutionStrategy.Parallel)]
        [InlineData(ExecutionStrategy.Serial)]
        [Theory]
        public void ExtendedRootTypesWillHonorSerialAttribute(ExecutionStrategy defaultStrategy)
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(c => c.Name(OperationTypeNames.Query))
                .AddType<FooQueries>()
                .ModifyOptions(o => o.DefaultResolverStrategy = defaultStrategy)
                .Create();

            DocumentNode document = Utf8GraphQLParser.Parse(
                @"{
                    foos
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
            Snapshot(root, defaultStrategy);
        }

        [InlineData(ExecutionStrategy.Parallel)]
        [InlineData(ExecutionStrategy.Serial)]
        [Theory]
        public void ExtendedRootTypesWillHonorGlobalSerialSetting(ExecutionStrategy defaultStrategy)
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(c => c.Name(OperationTypeNames.Query))
                .AddType<BarQueries>()
                .ModifyOptions(o => o.DefaultResolverStrategy = defaultStrategy)
                .Create();

            DocumentNode document = Utf8GraphQLParser.Parse(
                @"{
                    bars
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
            Snapshot(root, defaultStrategy);
        }

        private static void Snapshot(
            QueryPlanNode node, 
            ExecutionStrategy strategy = ExecutionStrategy.Parallel)
        {
            var options = new JsonWriterOptions { Indented = true };
            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream, options);

            node.Serialize(writer);
            writer.Flush();

            Encoding.UTF8
                .GetString(stream.ToArray())
                .MatchSnapshot(new SnapshotNameExtension(strategy));
        }

        [ExtendObjectType(OperationTypeNames.Query)]
        public class FooQueries 
        {
            [Serial]
            public IQueryable<string> GetFoos() => new [] { "a", "b" }.AsQueryable();
        }

        [ExtendObjectType(OperationTypeNames.Query)]
        public class BarQueries 
        {
            public IQueryable<string> GetBars() => new [] { "a", "b" }.AsQueryable();
        }
    }
}
