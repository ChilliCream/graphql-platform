using System.Text.Json.Nodes;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Lodash
{
    public class MapTests : LodashTestBase
    {
        [Fact]
        public async Task Map_OnList_MatchSnapshot()
        {
            // arrange
            IRequestExecutor? executor = await CreateExecutor();
            string query =
                @"
                {
                    list @map(key: ""baz""){
                        baz
                    }
                }";

            DocumentNode parsed = Utf8GraphQLParser.Parse(query);
            IReadOnlyQueryRequest request = QueryRequestBuilder
                .New()
                .SetQuery(parsed.RemoveLodash(executor.Schema))
                .Create();
            IExecutionResult result = await executor.ExecuteAsync(request);

            // act
            AggregationJsonRewriter lodashRewriter = parsed.CreateRewriter(executor.Schema);
            JsonNode? data = JsonNode.Parse(result.ToJson())?.AsObject()["data"];
            JsonNode? rewritten = lodashRewriter.Rewrite(data);

            // assert
            Assert.NotNull(rewritten);
            rewritten?.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task Map_OnScalar_MatchSnapshot()
        {
            // arrange
            IRequestExecutor? executor = await CreateExecutor();
            string query =
                @"
                {
                    single @map(key: ""baz""){
                        baz
                    }
                }";

            DocumentNode parsed = Utf8GraphQLParser.Parse(query);
            IReadOnlyQueryRequest request = QueryRequestBuilder
                .New()
                .SetQuery(parsed.RemoveLodash(executor.Schema))
                .Create();
            IExecutionResult result = await executor.ExecuteAsync(request);

            // act
            AggregationJsonRewriter lodashRewriter = parsed.CreateRewriter(executor.Schema);
            JsonNode? data = JsonNode.Parse(result.ToJson())?.AsObject()["data"];
            JsonNode? rewritten = lodashRewriter.Rewrite(data);

            // assert
            Assert.NotNull(rewritten);
            rewritten?.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task Map_OnListDeep_MatchSnapshot()
        {
            // arrange
            IRequestExecutor? executor = await CreateExecutor();
            string query =
                @"
                {
                    list {
                        bar {
                            foos @map(key: ""baz"") {
                                baz
                            }
                        }
                    }
                }";

            DocumentNode parsed = Utf8GraphQLParser.Parse(query);
            IReadOnlyQueryRequest request = QueryRequestBuilder
                .New()
                .SetQuery(parsed.RemoveLodash(executor.Schema))
                .Create();
            IExecutionResult result = await executor.ExecuteAsync(request);

            // act
            AggregationJsonRewriter lodashRewriter = parsed.CreateRewriter(executor.Schema);
            JsonNode? data = JsonNode.Parse(result.ToJson())?.AsObject()["data"];
            JsonNode? rewritten = lodashRewriter.Rewrite(data);

            // assert
            Assert.NotNull(rewritten);
            rewritten?.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task Map_OnObjectDeep_MatchSnapshot()
        {
            // arrange
            IRequestExecutor? executor = await CreateExecutor();
            string query =
                @"
                {
                    list {
                        bar {
                            foos {
                                bar @map(key: ""baz"") {
                                    baz
                                }
                            }
                        }
                    }
                }";

            DocumentNode parsed = Utf8GraphQLParser.Parse(query);
            IReadOnlyQueryRequest request = QueryRequestBuilder
                .New()
                .SetQuery(parsed.RemoveLodash(executor.Schema))
                .Create();
            IExecutionResult result = await executor.ExecuteAsync(request);

            // act
            AggregationJsonRewriter lodashRewriter = parsed.CreateRewriter(executor.Schema);
            JsonNode? data = JsonNode.Parse(result.ToJson())?.AsObject()["data"];
            JsonNode? rewritten = lodashRewriter.Rewrite(data);

            // assert
            Assert.NotNull(rewritten);
            rewritten?.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task Map_OnNestedList_MatchSnapshot()
        {
            // arrange
            IRequestExecutor? executor = await CreateExecutor();
            string query =
                @"
                {
                    nestedList @map(key: ""baz""){
                        baz
                    }
                }";

            DocumentNode parsed = Utf8GraphQLParser.Parse(query);
            IReadOnlyQueryRequest request = QueryRequestBuilder
                .New()
                .SetQuery(parsed.RemoveLodash(executor.Schema))
                .Create();
            IExecutionResult result = await executor.ExecuteAsync(request);

            // act
            AggregationJsonRewriter lodashRewriter = parsed.CreateRewriter(executor.Schema);
            JsonNode? data = JsonNode.Parse(result.ToJson())?.AsObject()["data"];
            JsonNode? rewritten = lodashRewriter.Rewrite(data);

            // assert
            Assert.NotNull(rewritten);
            rewritten?.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task Map_OnNullableList_MatchSnapshot()
        {
            // arrange
            IRequestExecutor? executor = await CreateExecutor();
            string query =
                @"
                {
                    nullableList @map(key: ""baz""){
                        baz
                    }
                }";

            DocumentNode parsed = Utf8GraphQLParser.Parse(query);
            IReadOnlyQueryRequest request = QueryRequestBuilder
                .New()
                .SetQuery(parsed.RemoveLodash(executor.Schema))
                .Create();
            IExecutionResult result = await executor.ExecuteAsync(request);

            // act
            AggregationJsonRewriter lodashRewriter = parsed.CreateRewriter(executor.Schema);
            JsonNode? data = JsonNode.Parse(result.ToJson())?.AsObject()["data"];
            JsonNode? rewritten = lodashRewriter.Rewrite(data);

            // assert
            Assert.NotNull(rewritten);
            rewritten?.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task Map_OnNullableScalar_MatchSnapshot()
        {
            // arrange
            IRequestExecutor? executor = await CreateExecutor();
            string query =
                @"
                {
                    nullable @map(key: ""baz""){
                        baz
                    }
                }";

            DocumentNode parsed = Utf8GraphQLParser.Parse(query);
            IReadOnlyQueryRequest request = QueryRequestBuilder
                .New()
                .SetQuery(parsed.RemoveLodash(executor.Schema))
                .Create();
            IExecutionResult result = await executor.ExecuteAsync(request);

            // act
            AggregationJsonRewriter lodashRewriter = parsed.CreateRewriter(executor.Schema);
            JsonNode? data = JsonNode.Parse(result.ToJson())?.AsObject()["data"];
            JsonNode? rewritten = lodashRewriter.Rewrite(data);

            // assert
            Assert.NotNull(rewritten);
            rewritten?.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task Map_OnNullableListDeep_MatchSnapshot()
        {
            // arrange
            IRequestExecutor? executor = await CreateExecutor();
            string query =
                @"
                {
                    nullableList {
                        bar {
                            foos @map(key: ""baz"") {
                                baz
                            }
                        }
                    }
                }";

            DocumentNode parsed = Utf8GraphQLParser.Parse(query);
            IReadOnlyQueryRequest request = QueryRequestBuilder
                .New()
                .SetQuery(parsed.RemoveLodash(executor.Schema))
                .Create();
            IExecutionResult result = await executor.ExecuteAsync(request);

            // act
            AggregationJsonRewriter lodashRewriter = parsed.CreateRewriter(executor.Schema);
            JsonNode? data = JsonNode.Parse(result.ToJson())?.AsObject()["data"];
            JsonNode? rewritten = lodashRewriter.Rewrite(data);

            // assert
            Assert.NotNull(rewritten);
            rewritten?.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task Map_OnNullableObjectDeep_MatchSnapshot()
        {
            // arrange
            IRequestExecutor? executor = await CreateExecutor();
            string query =
                @"
                {
                    nullableList {
                        bar {
                            foos {
                                bar @map(key: ""baz"") {
                                    baz
                                }
                            }
                        }
                    }
                }";

            DocumentNode parsed = Utf8GraphQLParser.Parse(query);
            IReadOnlyQueryRequest request = QueryRequestBuilder
                .New()
                .SetQuery(parsed.RemoveLodash(executor.Schema))
                .Create();
            IExecutionResult result = await executor.ExecuteAsync(request);

            // act
            AggregationJsonRewriter lodashRewriter = parsed.CreateRewriter(executor.Schema);
            JsonNode? data = JsonNode.Parse(result.ToJson())?.AsObject()["data"];
            JsonNode? rewritten = lodashRewriter.Rewrite(data);

            // assert
            Assert.NotNull(rewritten);
            rewritten?.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task Map_OnNullableNestedList_MatchSnapshot()
        {
            // arrange
            IRequestExecutor? executor = await CreateExecutor();
            string query =
                @"
                {
                    nullableNestedList @map(key: ""baz""){
                        baz
                    }
                }";

            DocumentNode parsed = Utf8GraphQLParser.Parse(query);
            IReadOnlyQueryRequest request = QueryRequestBuilder
                .New()
                .SetQuery(parsed.RemoveLodash(executor.Schema))
                .Create();
            IExecutionResult result = await executor.ExecuteAsync(request);

            // act
            AggregationJsonRewriter lodashRewriter = parsed.CreateRewriter(executor.Schema);
            JsonNode? data = JsonNode.Parse(result.ToJson())?.AsObject()["data"];
            JsonNode? rewritten = lodashRewriter.Rewrite(data);

            // assert
            Assert.NotNull(rewritten);
            rewritten?.ToString().MatchSnapshot();
        }
    }
}
