using System.Linq;
using HotChocolate.Execution;
using NodaTime.Text;
using Xunit;

namespace HotChocolate.Types.NodaTime.Tests
{
    public class LocalTimeTypeGeneralIsoIntegrationTests
    {
        private readonly IRequestExecutor testExecutor;
        public LocalTimeTypeGeneralIsoIntegrationTests()
        {
            testExecutor = SchemaBuilder.New()
                .AddQueryType<LocalTimeTypeIntegrationTests.Schema.Query>()
                .AddMutationType<LocalTimeTypeIntegrationTests.Schema.Mutation>()
                .AddNodaTime(typeof(LocalTimeType))
                .AddType(new LocalTimeType(LocalTimePattern.GeneralIso))
                .Create()
                .MakeExecutable();
        }

        [Fact]
        public void QueryReturns()
        {
            IExecutionResult? result = testExecutor.Execute("query { test: one }");
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("12:42:13", queryResult!.Data!["test"]);
        }

        [Fact]
        public void ParsesVariable()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: LocalTime!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "12:42:13")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("12:52:13", queryResult!.Data!["test"]);
        }

        [Fact]
        public void DoesntParseAnIncorrectVariable()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: LocalTime!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "12:42")
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
                    .SetQuery("mutation { test(arg: \"12:42:13\") }")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("12:52:13", queryResult!.Data!["test"]);
        }

        [Fact]
        public void DoesntParseIncorrectLiteral()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"12:42\") }")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Null(queryResult!.Data);
            Assert.Equal(1, queryResult!.Errors!.Count);
            Assert.Null(queryResult.Errors[0].Code);
            Assert.Equal(
                "Unable to deserialize string to LocalTime",
                queryResult.Errors[0].Message);
        }
    }
}
