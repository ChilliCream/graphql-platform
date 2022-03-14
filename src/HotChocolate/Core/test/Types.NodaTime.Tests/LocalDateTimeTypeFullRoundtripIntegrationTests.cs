using System.Linq;
using HotChocolate.Execution;
using NodaTime.Text;
using Xunit;

namespace HotChocolate.Types.NodaTime.Tests
{
    public class LocalDateTimeTypeFullRoundtripIntegrationTests
    {
        private readonly IRequestExecutor testExecutor;

        public LocalDateTimeTypeFullRoundtripIntegrationTests()
        {
            testExecutor = SchemaBuilder.New()
                .AddQueryType<LocalDateTimeTypeIntegrationTests.Schema.Query>()
                .AddMutationType<LocalDateTimeTypeIntegrationTests.Schema.Mutation>()
                .AddNodaTime(typeof(LocalDateTimeType))
                .AddType(new LocalDateTimeType(LocalDateTimePattern.FullRoundtrip))
                .Create()
                .MakeExecutable();
        }

        [Fact]
        public void QueryReturns()
        {
            IExecutionResult? result = testExecutor.Execute("query { test: one }");
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("2020-02-07T17:42:59.000001234 (Julian)", queryResult!.Data!["test"]);
        }

        [Fact]
        public void ParsesVariable()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: LocalDateTime!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "2020-02-21T17:42:59.000001234 (Julian)")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("2020-02-21T17:52:59.000001234 (Julian)", queryResult!.Data!["test"]);
        }

        [Fact]
        public void DoesntParseAnIncorrectVariable()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: LocalDateTime!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "2020-02-20T17:42:59Z")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Null(queryResult!.Data);
            Assert.Equal(1, queryResult!.Errors!.Count);
        }

        [Fact]
        public void ParsesLiteral()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"2020-02-20T17:42:59.000001234 (Julian)\") }")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("2020-02-20T17:52:59.000001234 (Julian)", queryResult!.Data!["test"]);
        }

        [Fact]
        public void DoesntParseIncorrectLiteral()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"2020-02-20T17:42:59Z\") }")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Null(queryResult!.Data);
            Assert.Equal(1, queryResult!.Errors!.Count);
            Assert.Null(queryResult.Errors[0].Code);
            Assert.Equal(
                "Unable to deserialize string to LocalDateTime",
                queryResult.Errors[0].Message);
        }
    }
}
