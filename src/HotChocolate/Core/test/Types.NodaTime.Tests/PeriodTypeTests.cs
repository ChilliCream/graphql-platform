using System;
using System.Collections.Generic;
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

        private readonly IRequestExecutor _testExecutor =
            SchemaBuilder.New()
                .AddQueryType<Schema.Query>()
                .AddMutationType<Schema.Mutation>()
                .AddNodaTime()
                .Create()
                .MakeExecutable();

        [Fact]
        public void QueryReturns()
        {
            var result = _testExecutor.Execute("query { test: one }");
            Assert.Equal("P-3W3DT139t", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void ParsesVariable()
        {
            var result = _testExecutor
                .Execute(OperationRequestBuilder.Create()
                    .SetDocument("mutation($arg: Period!) { test(arg: $arg) }")
                    .SetVariableValues(new Dictionary<string, object?> { {"arg", "P-3W15DT139t" }, })
                    .Build());
            Assert.Equal("P-3W15DT-10M139t", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void DoesntParseAnIncorrectVariable()
        {
            var result = _testExecutor
                .Execute(OperationRequestBuilder.Create()
                    .SetDocument("mutation($arg: Period!) { test(arg: $arg) }")
                    .SetVariableValues(new Dictionary<string, object?> { {"arg", "-3W3DT-10M139t" }, })
                    .Build());
            Assert.Null(result.ExpectQueryResult().Data);
            Assert.Equal(1, result.ExpectQueryResult().Errors!.Count);
        }

        [Fact]
        public void ParsesLiteral()
        {
            var result = _testExecutor
                .Execute(OperationRequestBuilder.Create()
                    .SetDocument("mutation { test(arg: \"P-3W15DT139t\") }")
                    .Build());
            Assert.Equal("P-3W15DT-10M139t", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void DoesntParseIncorrectLiteral()
        {
            var result = _testExecutor
                .Execute(OperationRequestBuilder.Create()
                    .SetDocument("mutation { test(arg: \"-3W3DT-10M139t\") }")
                    .Build());
            Assert.Null(result.ExpectQueryResult().Data);
            Assert.Equal(1, result.ExpectQueryResult().Errors!.Count);
            Assert.Null(result.ExpectQueryResult().Errors![0].Code);
            Assert.Equal(
                "Unable to deserialize string to Period",
                result.ExpectQueryResult().Errors![0].Message);
        }

        [Fact]
        public void PatternEmptyThrowSchemaException()
        {
            static object Call() => new PeriodType(Array.Empty<IPattern<Period>>());
            Assert.Throws<SchemaException>(Call);
        }
    }
}
