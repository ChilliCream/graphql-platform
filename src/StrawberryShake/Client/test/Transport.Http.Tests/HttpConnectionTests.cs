using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Utilities;
using Microsoft.AspNetCore.TestHost;
using Snapshooter.Xunit;
using Xunit;

namespace StrawberryShake.Transport.Http
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
            var connection = new HttpConnection(() => client);
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

            public DocumentHash Hash { get; } = new("MD5", "ABC");
        }
    }
}
