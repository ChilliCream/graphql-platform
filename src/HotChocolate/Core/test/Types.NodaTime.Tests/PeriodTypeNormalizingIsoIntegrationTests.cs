using System.Linq;
using HotChocolate.Execution;
using NodaTime.Text;
using Xunit;

namespace HotChocolate.Types.NodaTime.Tests
{
    public class PeriodTypeNormalizingIsoIntegrationTests
    {
        private readonly IRequestExecutor testExecutor;

        public PeriodTypeNormalizingIsoIntegrationTests()
        {
            testExecutor = SchemaBuilder.New()
                .AddQueryType<PeriodTypeIntegrationTests.Schema.Query>()
                .AddMutationType<PeriodTypeIntegrationTests.Schema.Mutation>()
                .AddNodaTime(typeof(PeriodType))
                .AddType(new PeriodType(PeriodPattern.NormalizingIso))
                .Create()
                .MakeExecutable();
        }

        [Fact]
        public void QueryReturns()
        {
            IExecutionResult? result = testExecutor.Execute("query { test: one }");
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("P-17DT-23H-59M-59.9999861S", queryResult!.Data!["test"]);
        }

        [Fact]
        public void ParsesVariable()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: Period!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "P-17DT-23H-59M-59.9999861S")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("P-18DT-9M-59.9999861S", queryResult!.Data!["test"]);
        }

        [Fact]
        public void DoesntParseAnIncorrectVariable()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: Period!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "-P-17DT-23H-59M-59.9999861S")
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
                    .SetQuery("mutation { test(arg: \"P-17DT-23H-59M-59.9999861S\") }")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("P-18DT-9M-59.9999861S", queryResult!.Data!["test"]);
        }

        [Fact]
        public void DoesntParseIncorrectLiteral()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"-P-17DT-23H-59M-59.9999861S\") }")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Null(queryResult!.Data);
            Assert.Equal(1, queryResult!.Errors!.Count);
            Assert.Null(queryResult.Errors[0].Code);
            Assert.Equal("Unable to deserialize string to Period", queryResult.Errors[0].Message);
        }
    }
}
