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
            QueryRequest request = CreateRequest();
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
               @"
                type Foo
                {
                    a: String!
                    b: String
                    c: Int
                }

                type Query {
                    getFoo: Foo
                }
                ",

                cnf =>
                {
                    cnf.BindResolver(() => "something").To("Query", "getFoo");
                    cnf.BindResolver(() => "hello").To("Foo", "a");
                    cnf.BindResolver(() => "world").To("Foo", "b");
                    cnf.BindResolver(() => 123).To("Foo", "c");
                }
           );
        }

        private QueryRequest CreateRequest()
        {
            return new QueryRequest
            {
                Query = "{ getFoo { a } }"
            };
        }
    }
}
