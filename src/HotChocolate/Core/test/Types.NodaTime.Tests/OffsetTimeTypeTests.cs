using System;
using HotChocolate.Execution;
using NodaTime;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime.Tests
{
    public class OffsetTimeTypeIntegrationTests
    {
        public static class Schema
        {
            public class Query
            {
                public OffsetTime Hours =>
                    new OffsetTime(
                        LocalTime
                            .FromHourMinuteSecondMillisecondTick(18, 30, 13, 10, 100)
                            .PlusNanoseconds(1234),
                        Offset.FromHours(2));

                public OffsetTime HoursAndMinutes =>
                    new OffsetTime(
                        LocalTime
                            .FromHourMinuteSecondMillisecondTick(18, 30, 13, 10, 100)
                            .PlusNanoseconds(1234),
                        Offset.FromHoursAndMinutes(2, 35));
            }

            public class Mutation
            {
                public OffsetTime Test(OffsetTime arg) => arg;
            }
        }

        private readonly IRequestExecutor _testExecutor;

        public OffsetTimeTypeIntegrationTests()
        {
            _testExecutor = SchemaBuilder.New()
                .AddQueryType<Schema.Query>()
                .AddMutationType<Schema.Mutation>()
                .AddNodaTime()
                .Create()
                .MakeExecutable();
        }

        [Fact]
        public void QueryReturns()
        {
            IExecutionResult? result = _testExecutor.Execute("query { test: hours }");
            Assert.Equal("18:30:13+02", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void QueryReturnsWithMinutes()
        {
            IExecutionResult? result = _testExecutor.Execute("query { test: hoursAndMinutes }");
            Assert.Equal("18:30:13+02:35", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void ParsesVariable()
        {
            IExecutionResult? result = _testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: OffsetTime!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "18:30:13+02")
                    .Create());
            Assert.Equal("18:30:13+02", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void ParsesVariableWithMinutes()
        {
            IExecutionResult? result = _testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: OffsetTime!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "18:30:13+02:35")
                    .Create());
            Assert.Equal("18:30:13+02:35", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void DoesntParseAnIncorrectVariable()
        {
            IExecutionResult? result = _testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: OffsetTime!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "18:30:13")
                    .Create());
            Assert.Null(result.ExpectQueryResult().Data);
            Assert.Equal(1, result.ExpectQueryResult().Errors!.Count);
        }

        [Fact]
        public void ParsesLiteral()
        {
            IExecutionResult? result = _testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"18:30:13+02\") }")
                    .Create());
            Assert.Equal("18:30:13+02", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void ParsesLiteralWithMinutes()
        {
            IExecutionResult? result = _testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"18:30:13+02:35\") }")
                    .Create());
            Assert.Equal("18:30:13+02:35", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void DoesntParseIncorrectLiteral()
        {
            IExecutionResult? result = _testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"18:30:13\") }")
                    .Create());

            Assert.Null(result.ExpectQueryResult().Data);
            Assert.Equal(1, result.ExpectQueryResult().Errors!.Count);
            Assert.Null(result.ExpectQueryResult().Errors![0].Code);
            Assert.Equal(
                "Unable to deserialize string to OffsetTime",
                result.ExpectQueryResult().Errors![0].Message);
        }

        [Fact]
        public void PatternEmpty_ThrowSchemaException()
        {
            static object Call() => new OffsetTimeType(Array.Empty<IPattern<OffsetTime>>());
            Assert.Throws<SchemaException>(Call);
        }
    }
}
