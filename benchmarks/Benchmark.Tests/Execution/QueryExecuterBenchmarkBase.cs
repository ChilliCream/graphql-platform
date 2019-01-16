using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using HotChocolate.Execution;
using HotChocolate.Language;

namespace HotChocolate.Benchmark.Tests.Execution
{

    [CoreJob]
    [RPlotExporter, MemoryDiagnoser]
    public class QueryExecutorBenchmarkBase
    {
        private readonly Schema _schema;
        private readonly IQueryExecutor _queryExecutor;

        public QueryExecutorBenchmarkBase(int cacheSize)
        {
            _schema = SchemaFactory.Create();
            _queryExecutor = QueryExecutionBuilder.New()
                .UseDefaultPipeline()
                .AddQueryCache(cacheSize)
                .Build(_schema);
        }

        [Benchmark]
        public async Task<IExecutionResult> GraphQLOrgFieldExample()
        {
            string query = @"
            {
                hero {
                    name
                    # Queries can have comments!
                    friends {
                        name
                    }
                }
            }";

            return await _queryExecutor.ExecuteAsync(
                new QueryRequest(query),
                CancellationToken.None);
        }

        [Benchmark]
        public async Task<IExecutionResult> GraphQLOrgFieldArgumentExample1()
        {
            string query = @"
            {
                human(id: ""1000"") {
                    name
                    height
                }
            }";

            return await _queryExecutor.ExecuteAsync(
                new QueryRequest(query),
                CancellationToken.None);
        }

        [Benchmark]
        public async Task<IExecutionResult> GraphQLOrgFieldArgumentExample2()
        {
            string query = @"
            {
                human(id: ""1000"") {
                    name
                    height(unit: FOOT)
                }
            }";

            return await _queryExecutor.ExecuteAsync(
                new QueryRequest(query),
                CancellationToken.None);
        }

        [Benchmark]
        public async Task<IExecutionResult> GraphQLOrgAliasExample()
        {
            string query = @"
            {
                empireHero: hero(episode: EMPIRE) {
                    name
                }
                jediHero: hero(episode: JEDI) {
                    name
                }
            }";

            return await _queryExecutor.ExecuteAsync(
                new QueryRequest(query),
                CancellationToken.None);
        }

        [Benchmark]
        public async Task<IExecutionResult> GraphQLOrgFragmentExample()
        {
            string query = @"
            {
                leftComparison: hero(episode: EMPIRE) {
                    ...comparisonFields
                }
                rightComparison: hero(episode: JEDI) {
                    ...comparisonFields
                }
                }

                fragment comparisonFields on Character {
                name
                appearsIn
                friends {
                    name
                }
            }";

            return await _queryExecutor.ExecuteAsync(
                new QueryRequest(query),
                CancellationToken.None);
        }

        [Benchmark]
        public async Task<IExecutionResult> GraphQLOrgOperationNameExample()
        {
            string query = @"
            query HeroNameAndFriends {
                hero {
                    name
                    friends {
                        name
                    }
                }
            }";

            return await _queryExecutor.ExecuteAsync(
                new QueryRequest(query),
                CancellationToken.None);
        }

        [Benchmark]
        public async Task<IExecutionResult> GraphQLOrgVariableExample()
        {
            // arrange
            string query = @"
            query HeroNameAndFriends($episode: Episode) {
                hero(episode: $episode) {
                    name
                    friends {
                        name
                    }
                }
            }";

            var variables = new Dictionary<string, object>
            {
                { "episode", new EnumValueNode("JEDI") }
            };

            return await _queryExecutor.ExecuteAsync(
                new QueryRequest(query) { VariableValues = variables },
                CancellationToken.None);
        }

        [Benchmark]
        public async Task<IExecutionResult> GraphQLOrgVariableWithDefaultValueExample()
        {
            string query = @"
            query HeroNameAndFriends($episode: Episode = JEDI) {
                hero(episode: $episode) {
                    name
                    friends {
                        name
                    }
                }
            }";

            return await _queryExecutor.ExecuteAsync(
                new QueryRequest(query),
                CancellationToken.None);
        }

        [Benchmark]
        public async Task<IExecutionResult> GraphQLOrgDirectiveIncludeExample1()
        {
            string query = @"
            query Hero($episode: Episode, $withFriends: Boolean!) {
                hero(episode: $episode) {
                    name
                    friends @include(if: $withFriends) {
                        name
                    }
                }
            }";

            var variables = new Dictionary<string, object>
            {
                { "episode", new EnumValueNode("JEDI") },
                { "withFriends", new BooleanValueNode(false) }
            };

            return await _queryExecutor.ExecuteAsync(
                new QueryRequest(query) { VariableValues = variables },
                CancellationToken.None);
        }

        [Benchmark]
        public async Task<IExecutionResult> GraphQLOrgDirectiveIncludeExample2()
        {
            string query = @"
            query Hero($episode: Episode, $withFriends: Boolean!) {
                hero(episode: $episode) {
                    name
                    friends @include(if: $withFriends) {
                        name
                    }
                }
            }";

            var variables = new Dictionary<string, object>
            {
                { "episode", new EnumValueNode("JEDI") },
                { "withFriends", new BooleanValueNode(true) }
            };

            return await _queryExecutor.ExecuteAsync(
                new QueryRequest(query) { VariableValues = variables },
                CancellationToken.None);
        }

        [Benchmark]
        public async Task<IExecutionResult> GraphQLOrgDirectiveSkipExample1()
        {
            string query = @"
            query Hero($episode: Episode, $withFriends: Boolean!) {
                hero(episode: $episode) {
                    name
                    friends @skip(if: $withFriends) {
                        name
                    }
                }
            }";

            var variables = new Dictionary<string, object>
            {
                { "episode", new EnumValueNode("JEDI") },
                { "withFriends", new BooleanValueNode(false) }
            };

            return await _queryExecutor.ExecuteAsync(
                new QueryRequest(query) { VariableValues = variables },
                CancellationToken.None);
        }

        [Benchmark]
        public async Task<IExecutionResult> GraphQLOrgDirectiveSkipExample2()
        {
            string query = @"
            query Hero($episode: Episode, $withFriends: Boolean!) {
                hero(episode: $episode) {
                    name
                    friends @skip(if: $withFriends) {
                        name
                    }
                }
            }";

            var variables = new Dictionary<string, object>
            {
                { "episode", new EnumValueNode("JEDI") },
                { "withFriends", new BooleanValueNode(true) }
            };

            return await _queryExecutor.ExecuteAsync(
                new QueryRequest(query) { VariableValues = variables },
                CancellationToken.None);
        }

        [Benchmark]
        public async Task<IExecutionResult> GraphQLOrgMutationExample()
        {
            string query = @"
            mutation CreateReviewForEpisode($ep: Episode!, $review: ReviewInput!) {
                createReview(episode: $ep, review: $review) {
                    stars
                    commentary
                }
            }";

            var variables = new Dictionary<string, object>
            {
                { "ep", new EnumValueNode("JEDI") },
                { "review", new ObjectValueNode(
                        new ObjectFieldNode("stars", new IntValueNode(5)),
                        new ObjectFieldNode("commentary",
                            new StringValueNode("This is a great movie!"))) }
            };

            return await _queryExecutor.ExecuteAsync(
                new QueryRequest(query) { VariableValues = variables },
                CancellationToken.None);
        }

        [Benchmark]
        public async Task<IExecutionResult> GraphQLOrgInlineFragmentExample1()
        {
            string query = @"
            query HeroForEpisode($ep: Episode!) {
                hero(episode: $ep) {
                    name
                    ... on Droid {
                        primaryFunction
                    }
                    ... on Human {
                        height
                    }
                }
            }";

            var variables = new Dictionary<string, object>
            {
                { "ep", new EnumValueNode("JEDI") },
            };

            return await _queryExecutor.ExecuteAsync(
                new QueryRequest(query) { VariableValues = variables },
                CancellationToken.None);
        }

        [Benchmark]
        public async Task<IExecutionResult> GraphQLOrgInlineFragmentExample2()
        {
            string query = @"
            query HeroForEpisode($ep: Episode!) {
                hero(episode: $ep) {
                    name
                    ... on Droid {
                        primaryFunction
                    }
                    ... on Human {
                        height
                    }
                }
            }";

            var variables = new Dictionary<string, object>
            {
                { "ep", new EnumValueNode("EMPIRE") },
            };

            return await _queryExecutor.ExecuteAsync(
                new QueryRequest(query) { VariableValues = variables },
                CancellationToken.None);
        }

        [Benchmark]
        public async Task<IExecutionResult> GraphQLOrgMetaFieldAndUnionExample()
        {
            string query = @"
            {
                search(text: ""an"") {
                    __typename
                    ... on Human {
                        name
                        height
                    }
                    ... on Droid {
                        name
                        primaryFunction
                    }
                    ... on Starship {
                        name
                        length
                    }
                }
            }";

            return await _queryExecutor.ExecuteAsync(
                new QueryRequest(query),
                CancellationToken.None);
        }
    }
}

