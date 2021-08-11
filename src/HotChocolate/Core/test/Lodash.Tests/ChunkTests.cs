using System.Text.Json.Nodes;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Lodash
{
    public class ChunkTests : LodashTestBase
    {
        [Fact]
        public async Task Chunk_OnList_MatchSnapshot()
        {
            // arrange
            IRequestExecutor? executor = await CreateExecutor();
            string query =
                @"
                {
                    list @chunk(size: 2){
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
        public async Task Chunk_OnListSmallerLastChunk_MatchSnapshot()
        {
            // arrange
            IRequestExecutor? executor = await CreateExecutor();
            string query =
                @"
                {
                    list @chunk(size: 3){
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
