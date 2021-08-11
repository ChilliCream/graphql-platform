using System.Text.Json.Nodes;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Lodash
{
    public class TakeTests : LodashTestBase
    {
        [Fact]
        public async Task Take_OnList_MatchSnapshot()
        {
            // arrange
            IRequestExecutor? executor = await CreateExecutor();
            string query =
                @"
                {
                    list @take(count: 2){
                        countBy
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
        public async Task Take_OnListSmallerThanTaken_MatchSnapshot()
        {
            // arrange
            IRequestExecutor? executor = await CreateExecutor();
            string query =
                @"
                {
                    list @take(count: 10){
                        countBy
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
