using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate.Execution;
using HotChocolate.Stitching;
using Xunit;

namespace Core.Tests.Stitching
{
    public class HttpQueryExecuterTests
    {

        [Fact]
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

            var queryExecuter = new HttpQueryExecuter(client);

            // act
            IExecutionResult result = await queryExecuter.ExecuteAsync(
                new QueryRequest(queryText), CancellationToken.None);

            // assert
            result.Snapshot();
        }
    }
}
