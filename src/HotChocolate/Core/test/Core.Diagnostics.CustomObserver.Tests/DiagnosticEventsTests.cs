using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Execution.Instrumentation;
using Microsoft.Extensions.DiagnosticAdapter;
using Xunit;

namespace HotChocolate
{
    public class DiagnosticsEventsTests
    {
        [Fact]
        public async Task VerifyCustomDignosticObserverIsWorkingProper()
        {
            // arrange
            var events = new List<string>();

            Schema schema = CreateSchema();

            IQueryExecutor executor = QueryExecutionBuilder.New()
                .UseDefaultPipeline()
                .AddDiagnosticObserver(new CustomDiagnosticsObserver(events))
                .Build(schema);

            IReadOnlyQueryRequest request =
                QueryRequestBuilder.New()
                    .SetQuery("{ a }")
                    .Create();

            // act
            IExecutionResult result = await executor.ExecuteAsync(request);

            // assert
            Assert.Empty(result.Extensions);
            Assert.Collection(events,
                i => Assert.Equal("foo", i),
                i => Assert.Equal("bar", i));
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
                    ctx => "hello world " + ctx.Argument<string>("a"))
                    .To("Query", "b");
                c.BindResolver(
                    () => ResolverResult<string>
                        .CreateValue("hello world x"))
                    .To("Query", "x");
                c.BindResolver(
                    () => ResolverResult<string>
                        .CreateError("hello world y"))
                    .To("Query", "y");
                c.BindResolver(
                    async () => await Task.FromResult(
                        ResolverResult<string>
                            .CreateValue("hello world xasync")))
                    .To("Query", "xasync");
                c.BindResolver(
                    async () => await Task.FromResult(
                        ResolverResult<string>
                            .CreateError("hello world yasync")))
                    .To("Query", "yasync");
            });
        }

        private class CustomDiagnosticsObserver
            : IDiagnosticObserver
        {
            private readonly List<string> _events;

            public CustomDiagnosticsObserver(List<string> events)
            {
                _events = events;
            }

            [DiagnosticName("HotChocolate.Execution.Query")]
            public void QueryExecute()
            {
                // Required for enabling Query events.
            }

            [DiagnosticName("HotChocolate.Execution.Query.Start")]
            public void BeginQueryExecute(IQueryContext context)
            {
                _events.Add("foo");
            }

            [DiagnosticName("HotChocolate.Execution.Query.Stop")]
            public void EndQueryExecute(
                IQueryContext context,
                IExecutionResult result)
            {
                _events.Add("bar");
            }
        }
    }
}
