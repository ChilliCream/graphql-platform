using System.Text.Json.Nodes;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Lodash
{
    public class KeysTests : LodashTestBase
    {
        [Fact]
        public async Task Keys_OnList_MatchSnapshot()
        {
            // arrange
            IRequestExecutor? executor = await CreateExecutor();
            string query =
                @"
                {
                    single @_(keys: none){
                        countBy
                        id
                        baz
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
