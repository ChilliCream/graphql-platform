using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate.Execution;
using Xunit;

namespace HotChocolate.Stitching
{
    public class HttpQueryExecutorTests
    {

        [Fact(Skip = "The snapshot is changing to often ...")]
        public async Task ExecuteQueryAgainstRemoteSchema()
        {
            // arrange
            var queryText = @"{
                systems(last:5) {
                    edges {
                        node {
                            id
                        }
                    }
                }
            }";

            var client = new HttpClient();
            client.BaseAddress = new Uri(
                "http://api.catalysis-hub.org/graphql");

            var queryExecutor = new HttpQueryExecutor(client);

            // act
            IExecutionResult result = await queryExecutor.ExecuteAsync(
                new QueryRequest(queryText), CancellationToken.None);

            // assert
            result.Snapshot();
        }
    }
}
