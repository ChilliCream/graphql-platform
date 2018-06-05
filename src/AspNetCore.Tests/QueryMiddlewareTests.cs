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
        public async Task BasicTest()
        {
            // arrange
            Schema schema = CreateSchema();
            QueryRequest request = new QueryRequest { Query = "{ basic { a } }" };
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
        public async Task SendRequestWithArguments()
        {
            // arrange
            Schema schema = CreateSchema();
            QueryRequest request = new QueryRequest
            {
                Query = @"
                query test($a: String) {
                    withScalarArgument(a: $a) {
                        a
                    }
                }",
                Variables = new Dictionary<string, object>
                {
                    { "a", "a" }
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
            return Schema.Create(
                FileResource.Open("ServerSchema.graphql"),
                cnf =>
                {
                    cnf.BindType<Query>();
                    cnf.BindType<Foo>();
                    cnf.BindType<Foo>().To("FooInput");
                }
           );
        }
    }

    public class Query
    {
        public Foo GetBasic()
        {
            return new Foo
            {
                A = "1",
                B = "2",
                C = "3"
            };
        }

        public Foo GetWithScalarArgument(string a)
        {
            return new Foo
            {
                A = a,
                B = "2",
                C = "3"
            };
        }

        public Foo GetWithObjectArgument(Foo b)
        {
            return new Foo
            {
                A = b.A,
                B = "2",
                C = b.C
            };
        }
    }

    public class Foo
    {
        public string A { get; set; }
        public string B { get; set; }
        public string C { get; set; }
    }
}
