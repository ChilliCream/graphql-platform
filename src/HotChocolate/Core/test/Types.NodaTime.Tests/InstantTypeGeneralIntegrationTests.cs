using System.Linq;
using HotChocolate.Execution;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime.Tests
{
    public class InstantTypeGeneralIntegrationTests
    {
        private readonly IRequestExecutor testExecutor;

        public InstantTypeGeneralIntegrationTests()
        {
            testExecutor = SchemaBuilder.New()
                .AddQueryType<InstantTypeIntegrationTests.Schema.Query>()
                .AddMutationType<InstantTypeIntegrationTests.Schema.Mutation>()
                .AddNodaTime(typeof(InstantType))
                .AddType(new InstantType(InstantPattern.General))
                .Create()
                .MakeExecutable();
        }

        [Fact]
        public void QueryReturnsUtc()
        {
            IExecutionResult result = testExecutor.Execute("query { test: one }");

            Assert.Equal("2020-02-20T17:42:59Z", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void ParsesVariable()
        {
            IExecutionResult result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: Instant!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "2020-02-21T17:42:59Z")
                    .Create());

            Assert.Equal("2020-02-21T17:52:59Z", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void DoesntParseAnIncorrectVariable()
        {
            IExecutionResult result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: Instant!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "2020-02-20T17:42:59")
                    .Create());

            Assert.Null(result.ExpectQueryResult().Data);
            Assert.Equal(1, result.ExpectQueryResult().Errors!.Count);
        }

        [Fact]
        public void ParsesLiteral()
        {
            IExecutionResult result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"2020-02-20T17:42:59Z\") }")
                    .Create());

            Assert.Equal("2020-02-20T17:52:59Z", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void DoesntParseIncorrectLiteral()
        {
            IExecutionResult result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"2020-02-20T17:42:59\") }")
                    .Create());

            Assert.Null(result.ExpectQueryResult().Data);
            Assert.Equal(1, result.ExpectQueryResult().Errors!.Count);
            Assert.Null(result.ExpectQueryResult().Errors!.First().Code);
            Assert.Equal(
                "Unable to deserialize string to Instant",
                result.ExpectQueryResult().Errors!.First().Message);
        }
    }
}
