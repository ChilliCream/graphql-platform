using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.TestHost;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
            TestServer server = CreateTestServer();
            QueryRequestDto request = new QueryRequestDto { Query = "{ basic { a } }" };

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
        public async Task HttpPost_EnumArgument()
        {
            // arrange
            TestServer server = CreateTestServer();
            QueryRequestDto request = new QueryRequestDto
            {
                Query = "query a($a: TestEnum) { withEnum(test: $a) }",
                Variables = JObject.FromObject(new Dictionary<string, object>
                {
                    { "a", "A"}
                })
            };

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
        public async Task HttpPost_WithScalarVariables()
        {
            // arrange
            TestServer server = CreateTestServer();
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
                Variables = JObject.FromObject(new Dictionary<string, object>
                {
                    { "a", "1234567890"}
                })
            };

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
            TestServer server = CreateTestServer();
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
                Variables = JObject.FromObject(new Dictionary<string, object>
                {
                    { "a", new Dictionary<string, object> {
                        {"a", "44"},
                        {"b", "55"},
                        {"c", 66}
                    } }
                })
            };

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
        public async Task HttpPost_WithScopedService()
        {
            // arrange
            TestServer server = CreateTestServer();
            QueryRequestDto request = new QueryRequestDto
            {
                Query = @"
                {
                    sayHello
                }"
            };

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
        public async Task HttpPost_WithHttpContext()
        {
            // arrange
            TestServer server = CreateTestServer();
            QueryRequestDto request = new QueryRequestDto
            {
                Query = @"
                {
                    requestPath
                }"
            };

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
            TestServer server = CreateTestServer();
            string query = "{ basic { a } }";

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
        public async Task HttpGet_ForwardToNextMiddleware()
        {
            // arrange
            TestServer server = CreateTestServer();

            // act
            HttpResponseMessage message = await server.CreateClient()
                .GetAsync($"http://localhost:5000/1234");

            // assert
            Assert.Equal(HttpStatusCode.NotFound, message.StatusCode);
        }

        [Fact]
        public async Task HttpGet_IntrospectionTest()
        {
            // arrange
            TestServer server = CreateTestServer();
            Uri requestUri = new Uri("http://localhost:5000/?query=%0A%20%20%20%20query%20IntrospectionQuery%20%7B%0A%20%20%20%20%20%20__schema%20%7B%0A%20%20%20%20%20%20%20%20queryType%20%7B%20name%20%7D%0A%20%20%20%20%20%20%20%20mutationType%20%7B%20name%20%7D%0A%20%20%20%20%20%20%20%20subscriptionType%20%7B%20name%20%7D%0A%20%20%20%20%20%20%20%20types%20%7B%0A%20%20%20%20%20%20%20%20%20%20...FullType%0A%20%20%20%20%20%20%20%20%7D%0A%20%20%20%20%20%20%20%20directives%20%7B%0A%20%20%20%20%20%20%20%20%20%20name%0A%20%20%20%20%20%20%20%20%20%20description%0A%20%20%20%20%20%20%20%20%20%20locations%0A%20%20%20%20%20%20%20%20%20%20args%20%7B%0A%20%20%20%20%20%20%20%20%20%20%20%20...InputValue%0A%20%20%20%20%20%20%20%20%20%20%7D%0A%20%20%20%20%20%20%20%20%7D%0A%20%20%20%20%20%20%7D%0A%20%20%20%20%7D%0A%0A%20%20%20%20fragment%20FullType%20on%20__Type%20%7B%0A%20%20%20%20%20%20kind%0A%20%20%20%20%20%20name%0A%20%20%20%20%20%20description%0A%20%20%20%20%20%20fields(includeDeprecated%3A%20true)%20%7B%0A%20%20%20%20%20%20%20%20name%0A%20%20%20%20%20%20%20%20description%0A%20%20%20%20%20%20%20%20args%20%7B%0A%20%20%20%20%20%20%20%20%20%20...InputValue%0A%20%20%20%20%20%20%20%20%7D%0A%20%20%20%20%20%20%20%20type%20%7B%0A%20%20%20%20%20%20%20%20%20%20...TypeRef%0A%20%20%20%20%20%20%20%20%7D%0A%20%20%20%20%20%20%20%20isDeprecated%0A%20%20%20%20%20%20%20%20deprecationReason%0A%20%20%20%20%20%20%7D%0A%20%20%20%20%20%20inputFields%20%7B%0A%20%20%20%20%20%20%20%20...InputValue%0A%20%20%20%20%20%20%7D%0A%20%20%20%20%20%20interfaces%20%7B%0A%20%20%20%20%20%20%20%20...TypeRef%0A%20%20%20%20%20%20%7D%0A%20%20%20%20%20%20enumValues(includeDeprecated%3A%20true)%20%7B%0A%20%20%20%20%20%20%20%20name%0A%20%20%20%20%20%20%20%20description%0A%20%20%20%20%20%20%20%20isDeprecated%0A%20%20%20%20%20%20%20%20deprecationReason%0A%20%20%20%20%20%20%7D%0A%20%20%20%20%20%20possibleTypes%20%7B%0A%20%20%20%20%20%20%20%20...TypeRef%0A%20%20%20%20%20%20%7D%0A%20%20%20%20%7D%0A%0A%20%20%20%20fragment%20InputValue%20on%20__InputValue%20%7B%0A%20%20%20%20%20%20name%0A%20%20%20%20%20%20description%0A%20%20%20%20%20%20type%20%7B%20...TypeRef%20%7D%0A%20%20%20%20%20%20defaultValue%0A%20%20%20%20%7D%0A%0A%20%20%20%20fragment%20TypeRef%20on%20__Type%20%7B%0A%20%20%20%20%20%20kind%0A%20%20%20%20%20%20name%0A%20%20%20%20%20%20ofType%20%7B%0A%20%20%20%20%20%20%20%20kind%0A%20%20%20%20%20%20%20%20name%0A%20%20%20%20%20%20%20%20ofType%20%7B%0A%20%20%20%20%20%20%20%20%20%20kind%0A%20%20%20%20%20%20%20%20%20%20name%0A%20%20%20%20%20%20%20%20%20%20ofType%20%7B%0A%20%20%20%20%20%20%20%20%20%20%20%20kind%0A%20%20%20%20%20%20%20%20%20%20%20%20name%0A%20%20%20%20%20%20%20%20%20%20%20%20ofType%20%7B%0A%20%20%20%20%20%20%20%20%20%20%20%20%20%20kind%0A%20%20%20%20%20%20%20%20%20%20%20%20%20%20name%0A%20%20%20%20%20%20%20%20%20%20%20%20%20%20ofType%20%7B%0A%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20kind%0A%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20name%0A%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20ofType%20%7B%0A%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20kind%0A%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20name%0A%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20ofType%20%7B%0A%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20kind%0A%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20name%0A%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%7D%0A%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%20%7D%0A%20%20%20%20%20%20%20%20%20%20%20%20%20%20%7D%0A%20%20%20%20%20%20%20%20%20%20%20%20%7D%0A%20%20%20%20%20%20%20%20%20%20%7D%0A%20%20%20%20%20%20%20%20%7D%0A%20%20%20%20%20%20%7D%0A%20%20%20%20%7D%0A%20%20&variables=%22%7B%7D%22");

            // act
            HttpResponseMessage message = await server.SendGetRequestAsync(requestUri);

            // assert
            Assert.Equal(HttpStatusCode.OK, message.StatusCode);

            string json = await message.Content.ReadAsStringAsync();
            QueryResultDto result = JsonConvert.DeserializeObject<QueryResultDto>(json);
            Assert.Null(result.Errors);
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }

        private TestServer CreateTestServer()
        {
            return TestServerFactory.Create(
                c => c.RegisterQueryType<QueryType>(), null);
        }
    }
}
