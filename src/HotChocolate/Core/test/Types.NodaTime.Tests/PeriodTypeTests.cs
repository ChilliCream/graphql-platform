using System;
using HotChocolate.Execution;
using NodaTime;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime.Tests
{
    public class PeriodTypeIntegrationTests
    {
        public static class Schema
        {
            public class Query
            {
                public Period One =>
                    Period.FromWeeks(-3) + Period.FromDays(3) + Period.FromTicks(139);
            }

            public class Mutation
            {
                public Period Test(Period arg)
                    => arg + Period.FromMinutes(-10);
            }
        }

        private readonly IRequestExecutor testExecutor;

        public PeriodTypeIntegrationTests()
        {
            testExecutor = SchemaBuilder.New()
                .AddQueryType<Schema.Query>()
                .AddMutationType<Schema.Mutation>()
                .AddNodaTime()
                .Create()
                .MakeExecutable();
        }

        [Fact]
        public void QueryReturns()
        {
            IExecutionResult? result = testExecutor.Execute("query { test: one }");
            Assert.Equal("P-3W3DT139t", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void ParsesVariable()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: Period!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "P-3W15DT139t")
                    .Create());
            Assert.Equal("P-3W15DT-10M139t", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void DoesntParseAnIncorrectVariable()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: Period!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "-3W3DT-10M139t")
                    .Create());
            Assert.Null(result.ExpectQueryResult().Data);
            Assert.Equal(1, result.ExpectQueryResult().Errors!.Count);
        }

        [Fact]
        public void ParsesLiteral()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"P-3W15DT139t\") }")
                    .Create());
            Assert.Equal("P-3W15DT-10M139t", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void DoesntParseIncorrectLiteral()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"-3W3DT-10M139t\") }")
                    .Create());
            Assert.Null(result.ExpectQueryResult().Data);
            Assert.Equal(1, result.ExpectQueryResult().Errors!.Count);
            Assert.Null(result.ExpectQueryResult().Errors![0].Code);
            Assert.Equal(
                "Unable to deserialize string to Period",
                result.ExpectQueryResult().Errors![0].Message);
        }

        [Fact]
        public void PatternEmpty_ThrowSchemaException()
        {
            static object Call() => new PeriodType(Array.Empty<IPattern<Period>>());
            Assert.Throws<SchemaException>(Call);
        }
    }
}
