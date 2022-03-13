using System.Linq;
using HotChocolate.Execution;
using NodaTime.Text;
using Xunit;

namespace HotChocolate.Types.NodaTime.Tests
{
    public class OffsetTimeTypeRfc3339IntegrationTests
    {
        private readonly IRequestExecutor testExecutor;

        public OffsetTimeTypeRfc3339IntegrationTests()
        {
            testExecutor = SchemaBuilder.New()
                .AddQueryType<OffsetTimeTypeIntegrationTests.Schema.Query>()
                .AddMutationType<OffsetTimeTypeIntegrationTests.Schema.Mutation>()
                .AddNodaTime(typeof(OffsetTimeType))
                .AddType(new OffsetTimeType(OffsetTimePattern.Rfc3339))
                .Create()
                .MakeExecutable();
        }

        [Fact]
        public void QueryReturns()
        {
            IExecutionResult? result = testExecutor.Execute("query { test: hours }");
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("18:30:13.010011234+02:00", queryResult!.Data!["test"]);
        }

        [Fact]
        public void QueryReturnsWithMinutes()
        {
            IExecutionResult? result = testExecutor.Execute("query { test: hoursAndMinutes }");
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("18:30:13.010011234+02:35", queryResult!.Data!["test"]);
        }

        [Fact]
        public void ParsesVariable()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: OffsetTime!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "18:30:13.010011234+02:00")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("18:30:13.010011234+02:00", queryResult!.Data!["test"]);
        }

        [Fact]
        public void ParsesVariableWithMinutes()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: OffsetTime!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "18:30:13.010011234+02:35")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("18:30:13.010011234+02:35", queryResult!.Data!["test"]);
        }

        [Fact]
        public void DoesntParseAnIncorrectVariable()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: OffsetTime!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "18:30:13.010011234+02")
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
                    .SetQuery("mutation { test(arg: \"18:30:13.010011234+02:00\") }")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("18:30:13.010011234+02:00", queryResult!.Data!["test"]);
        }

        [Fact]
        public void ParsesLiteralWithMinutes()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"18:30:13.010011234+02:35\") }")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("18:30:13.010011234+02:35", queryResult!.Data!["test"]);
        }

        [Fact]
        public void DoesntParseIncorrectLiteral()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"18:30:13.010011234+02\") }")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Null(queryResult!.Data);
            Assert.Equal(1, queryResult!.Errors!.Count);
            Assert.Null(queryResult.Errors[0].Code);
            Assert.Equal(
                "Unable to deserialize string to OffsetTime",
                queryResult.Errors[0].Message);
        }
    }
}
