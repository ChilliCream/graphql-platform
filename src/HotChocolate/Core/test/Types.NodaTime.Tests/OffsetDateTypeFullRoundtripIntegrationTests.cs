using HotChocolate.Execution;
using NodaTime.Text;

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
            IExecutionResult result = testExecutor.Execute("query { test: hours }");

            Assert.Equal("2020-12-31+02 (Gregorian)", result.ExpectQueryResult()!.Data!["test"]);
        }

        [Fact]
        public void QueryReturnsWithMinutes()
        {
            IExecutionResult result = testExecutor.Execute("query { test: hoursAndMinutes }");

            Assert.Equal("2020-12-31+02:35 (Gregorian)", result.ExpectQueryResult()!.Data!["test"]);
        }

        [Fact]
        public void ParsesVariable()
        {
            IExecutionResult result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: OffsetDate!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "2020-12-31+02 (Gregorian)")
                    .Create());

            Assert.Equal("2020-12-31+02 (Gregorian)", result.ExpectQueryResult()!.Data!["test"]);
        }

        [Fact]
        public void ParsesVariableWithMinutes()
        {
            IExecutionResult result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: OffsetDate!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "2020-12-31+02:35 (Gregorian)")
                    .Create());

            Assert.Equal("2020-12-31+02:35 (Gregorian)", result.ExpectQueryResult()!.Data!["test"]);
        }

        [Fact]
        public void DoesntParseAnIncorrectVariable()
        {
            IExecutionResult result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: OffsetDate!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "2020-12-31 (Gregorian)")
                    .Create());

            Assert.Null(result.ExpectQueryResult()!.Data);
            Assert.Equal(1, result.ExpectQueryResult()!.Errors!.Count);
        }

        [Fact]
        public void ParsesLiteral()
        {
            IExecutionResult result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"2020-12-31+02 (Gregorian)\") }")
                    .Create());

            Assert.Equal("2020-12-31+02 (Gregorian)", result.ExpectQueryResult()!.Data!["test"]);
        }

        [Fact]
        public void ParsesLiteralWithMinutes()
        {
            IExecutionResult result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"2020-12-31+02:35 (Gregorian)\") }")
                    .Create());

            Assert.Equal("2020-12-31+02:35 (Gregorian)", result.ExpectQueryResult()!.Data!["test"]);
        }

        [Fact]
        public void DoesntParseIncorrectLiteral()
        {
            IExecutionResult result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"2020-12-31 (Gregorian)\") }")
                    .Create());

            Assert.Null(result.ExpectQueryResult()!.Data);
            Assert.Equal(1, result.ExpectQueryResult()!.Errors!.Count);
            Assert.Null(result.ExpectQueryResult().Errors![0].Code);
            Assert.Equal(
                "Unable to deserialize string to OffsetDate",
                result.ExpectQueryResult().Errors![0].Message);
        }
    }
}
