using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.TestHost;
using Newtonsoft.Json;
using Xunit;

namespace HotChocolate.AspNetCore
{
    public class QueryMiddlewareTests
         : IClassFixture<TestServerFactory>
    {
        public QueryMiddlewareTests(TestServerFactory testServerFactory)
        {
            TestServerFactory = testServerFactory;
        }

        private TestServerFactory TestServerFactory { get; set; }

        [Fact]
        public async Task HttpPost_BasicTest()
        {
            // arrange
            Schema schema = CreateSchema();
            QueryRequestDto request = new QueryRequestDto { Query = "{ basic { a } }" };
            TestServer server = TestServerFactory.Create(schema, null);

            // act
            HttpResponseMessage message = await server.SendRequestAsync(request);

            // assert
            Assert.Equal(HttpStatusCode.OK, message.StatusCode);

            string json = await message.Content.ReadAsStringAsync();
            QueryResultDto result = JsonConvert.DeserializeObject<QueryResultDto>(json);
            Assert.Null(result.Errors);
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }

        [Fact]
        public async Task HttpGet_BasicTest()
        {
            // arrange
            Schema schema = CreateSchema();
            string query = "{ basic { a } }";
            TestServer server = TestServerFactory.Create(schema, null);

            // act
            HttpResponseMessage message = await server.SendGetRequestAsync(query);

            // assert
            Assert.Equal(HttpStatusCode.OK, message.StatusCode);

            string json = await message.Content.ReadAsStringAsync();
            QueryResultDto result = JsonConvert.DeserializeObject<QueryResultDto>(json);
            Assert.Null(result.Errors);
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }

        [Fact]
        public async Task HttpPost_WithScalarVariables()
        {
            // arrange
            Schema schema = CreateSchema();
            QueryRequestDto request = new QueryRequestDto
            {
                Query = @"
                query test($a: String!) {
                    withScalarArgument(a: $a) {
                        a
                        b
                        c
                    }
                }",
                Variables = new Dictionary<string, object>
                {
                    { "a", "1234567890"}
                }
            };
            TestServer server = TestServerFactory.Create(schema, null);

            // act
            HttpResponseMessage message = await server.SendRequestAsync(request);

            // assert
            Assert.Equal(HttpStatusCode.OK, message.StatusCode);

            string json = await message.Content.ReadAsStringAsync();
            QueryResultDto result = JsonConvert.DeserializeObject<QueryResultDto>(json);
            Assert.Null(result.Errors);
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }

        [Fact]
        public async Task HttpPost_WithObjectVariables()
        {
            // arrange
            Schema schema = CreateSchema();
            QueryRequestDto request = new QueryRequestDto
            {
                Query = @"
                query test($a: FooInput!) {
                    withObjectArgument(b: $a) {
                        a
                        b
                        c
                    }
                }",
                Variables = new Dictionary<string, object>
                {
                    { "a", new Dictionary<string, object> {
                        {"a", "44"},
                        {"b", "55"},
                        {"c", 66}
                    } }
                }
            };
            TestServer server = TestServerFactory.Create(schema, null);

            // act
            HttpResponseMessage message = await server.SendRequestAsync(request);

            // assert
            Assert.Equal(HttpStatusCode.OK, message.StatusCode);

            string json = await message.Content.ReadAsStringAsync();
            QueryResultDto result = JsonConvert.DeserializeObject<QueryResultDto>(json);
            Assert.Null(result.Errors);
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }

        private Schema CreateSchema()
        {
            return Schema.Create(c => c.RegisterQueryType<QueryType>());
        }
    }
}
