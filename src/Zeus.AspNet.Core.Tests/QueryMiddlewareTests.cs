using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Zeus;
using Zeus.Execution;
using Zeus.Parser;
using Zeus.Resolvers;
using Xunit;

namespace Zeus.AspNet
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
        public async Task SimpleRequestNoRoute()
        {
            // arrange
            ISchema schema = CreateSchema();
            QueryRequest request = CreateRequest();
            TestServer server = TestServerFactory.Create(schema, null);

            // act
            HttpResponseMessage message = await server.SendRequestAsync(request);

            // assert
            Assert.Equal(HttpStatusCode.OK, message.StatusCode);

            string json = await message.Content.ReadAsStringAsync();
            QueryResultDto result = JsonConvert.DeserializeObject<QueryResultDto>(json);
            Assert.True(result.Data.ContainsKey("getFoo"));
            Assert.Null(result.Errors);
        }

        private ISchema CreateSchema()
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

               c => c
                   .Add("Query", "getFoo", () => "something")
                   .Add("Foo", "a", () => "hello")
                   .Add("Foo", "b", () => "world")
                   .Add("Foo", "c", () => 123)
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