using System.Text.Json.Nodes;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Lodash
{
    public class UniqTests : LodashTestBase
    {
        [Fact]
        public async Task Uniq_OnScalarList_MatchSnapshot()
        {
            // arrange
            IRequestExecutor? executor = await CreateExecutor();
            string query =
                @"
                {
                    scalars @uniq
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
        public async Task Uniq_OnList_MatchSnapshot()
        {
            // arrange
            IRequestExecutor? executor = await CreateExecutor();
            string query =
                @"
                {
                    list @map(key: ""num"") @uniq {
                        num
                        id
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
        public async Task Uniq_OnListOfObject_MatchSnapshot()
        {
            // arrange
            IRequestExecutor? executor = await CreateExecutor();
            string query =
                @"
                {
                    list @uniq {
                        num
                        id
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
