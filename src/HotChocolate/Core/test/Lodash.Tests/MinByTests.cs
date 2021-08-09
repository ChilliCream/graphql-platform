using System.Text.Json.Nodes;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Lodash
{
    public class MinByTests : LodashTestBase
    {
        [Fact]
        public async Task MinBy_OnList_MatchSnapshot()
        {
            // arrange
            IRequestExecutor? executor = await CreateExecutor();
            string query =
                @"
                {
                    list @_(minBy: ""num""){
                        num
                        id
                    }
                }";

            DocumentNode parsed = Utf8GraphQLParser.Parse(query);
            IReadOnlyQueryRequest request = QueryRequestBuilder
                .New()
                .SetQuery(parsed.RemoveLodash())
                .Create();
            IExecutionResult result = await executor.ExecuteAsync(request);

            // act
            LodashJsonRewriter lodashRewriter = parsed.CreateRewriter();
            JsonNode? data = JsonNode.Parse(result.ToJson())?.AsObject()["data"];
            JsonNode? rewritten = lodashRewriter.Rewrite(data);

            // assert
            Assert.NotNull(rewritten);
            rewritten?.ToString().MatchSnapshot();
        }
    }
}
