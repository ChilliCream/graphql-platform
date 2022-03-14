using System.Linq;
using HotChocolate.Execution;
using NodaTime.Text;
using Xunit;

namespace HotChocolate.Types.NodaTime.Tests
{
    public class OffsetDateTypeFullRoundtripIntegrationTests
    {
        private readonly IRequestExecutor testExecutor;

        public OffsetDateTypeFullRoundtripIntegrationTests()
        {
            testExecutor = SchemaBuilder.New()
                .AddQueryType<OffsetDateTypeIntegrationTests.Schema.Query>()
                .AddMutationType<OffsetDateTypeIntegrationTests.Schema.Mutation>()
                .AddNodaTime(typeof(OffsetDateType))
                .AddType(new OffsetDateType(OffsetDatePattern.FullRoundtrip))
                .Create()
                .MakeExecutable();
        }

        [Fact]
        public void QueryReturns()
        {
            IExecutionResult? result = testExecutor.Execute("query { test: hours }");
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("2020-12-31+02 (Gregorian)", queryResult!.Data!["test"]);
        }

        [Fact]
        public void QueryReturnsWithMinutes()
        {
            IExecutionResult? result = testExecutor.Execute("query { test: hoursAndMinutes }");
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("2020-12-31+02:35 (Gregorian)", queryResult!.Data!["test"]);
        }

        [Fact]
        public void ParsesVariable()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: OffsetDate!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "2020-12-31+02 (Gregorian)")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("2020-12-31+02 (Gregorian)", queryResult!.Data!["test"]);
        }

        [Fact]
        public void ParsesVariableWithMinutes()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: OffsetDate!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "2020-12-31+02:35 (Gregorian)")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("2020-12-31+02:35 (Gregorian)", queryResult!.Data!["test"]);
        }

        [Fact]
        public void DoesntParseAnIncorrectVariable()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: OffsetDate!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "2020-12-31 (Gregorian)")
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
                    .SetQuery("mutation { test(arg: \"2020-12-31+02 (Gregorian)\") }")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("2020-12-31+02 (Gregorian)", queryResult!.Data!["test"]);
        }

        [Fact]
        public void ParsesLiteralWithMinutes()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"2020-12-31+02:35 (Gregorian)\") }")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("2020-12-31+02:35 (Gregorian)", queryResult!.Data!["test"]);
        }

        [Fact]
        public void DoesntParseIncorrectLiteral()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"2020-12-31 (Gregorian)\") }")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Null(queryResult!.Data);
            Assert.Equal(1, queryResult!.Errors!.Count);
            Assert.Null(queryResult.Errors[0].Code);
            Assert.Equal(
                "Unable to deserialize string to OffsetDate",
                queryResult.Errors[0].Message);
        }
    }
}
