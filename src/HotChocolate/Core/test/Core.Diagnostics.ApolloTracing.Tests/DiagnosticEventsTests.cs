using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using Xunit;

namespace HotChocolate
{
    public class DiagnosticsEventsTests
    {
        [Fact]
        public async Task EnsureQueryResultContainsExtensionTracing()
        {
            // arrange
            Schema schema = CreateSchema();

            IQueryExecutor executor = QueryExecutionBuilder
                .BuildDefault(schema, new QueryExecutionOptions
                {
                    TracingPreference = TracingPreference.Always
                });

            var request = QueryRequestBuilder.New()
                .SetQuery("{ a }")
                .Create();

            // act
            IExecutionResult result = await executor.ExecuteAsync(request);

            // assert
            Assert.NotEmpty(result.Extensions);
            Assert.True(result.Extensions.ContainsKey("tracing"));
        }

        private Schema CreateSchema()
        {
            return Schema.Create(@"
                type Query {
                    a: String
                    b(a: String!): String
                    x: String
                    y: String
                    xasync: String
                    yasync: String
                }
                ", c =>
            {
                c.BindResolver(() => "hello world a")
                    .To("Query", "a");
                c.BindResolver(
                    ctx => "hello world " + ctx.ArgumentValue<string>("a"))
                    .To("Query", "b");
                c.BindResolver(
                    () => "hello world x")
                    .To("Query", "x");
                c.BindResolver(
                    () => "hello world y")
                    .To("Query", "y");
                c.BindResolver(
                    async () => await Task.FromResult("hello world xasync"))
                    .To("Query", "xasync");
                c.BindResolver(
                    async () => await Task.FromResult(
                        new Error("hello world yasync")))
                    .To("Query", "yasync");
            });
        }
    }
}
