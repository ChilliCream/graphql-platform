using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.TestHost;
using HotChocolate.AspNetCore.Utilities;
using Moq;
using Snapshooter.Xunit;
using StrawberryShake.Http;
using Xunit;

namespace StrawberryShake.Http
{
    public class HttpConnectionTests : ServerTestBase
    {
        public HttpConnectionTests(TestServerFactory serverFactory)
            : base(serverFactory)
        {
        }

        [Fact]
        public async Task Simple_Request()
        {
            // arrange
            TestServer server = CreateStarWarsServer();
            HttpClient client = server.CreateClient();
            client.BaseAddress = new Uri("http://localhost:5000/graphql");

            var document = new MockDocument("query Test { __typename }");
            var request = new OperationRequest("Test", document);

            // act
            var results = new List<JsonDocument>();
            var connection = new HttpConnection(client);
            await foreach (var response in connection.ExecuteAsync(request))
            {
                if (response.Body is not null)
                {
                    results.Add(response.Body);
                }
            }

            // assert
            Assert.Collection(
                results,
                t => t.RootElement.ToString().MatchSnapshot());
        }


        private class MockDocument : IDocument
        {
            private readonly byte[] _query;

            public MockDocument(string query)
            {
                _query = Encoding.UTF8.GetBytes(query);
            }

            public OperationKind Kind => OperationKind.Query;

            public ReadOnlySpan<byte> Body => _query;
        }
    }
}
