using HotChocolate.Execution;
using NodaTime;

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
            var result = _testExecutor.Execute("query { test: hours }");
            Assert.Equal("18:30:13+02", result.ExpectSingleResult().Data!["test"]);
        }

        [Fact]
        public void QueryReturnsWithMinutes()
        {
            var result = _testExecutor.Execute("query { test: hoursAndMinutes }");
            Assert.Equal("18:30:13+02:35", result.ExpectSingleResult().Data!["test"]);
        }

        [Fact]
        public void ParsesVariable()
        {
            var result = _testExecutor
                .Execute(OperationRequestBuilder.New()
                    .SetDocument("mutation($arg: OffsetTime!) { test(arg: $arg) }")
                    .SetVariableValues(new Dictionary<string, object?> { {"arg", "18:30:13+02" }, })
                    .Build());
            Assert.Equal("18:30:13+02", result.ExpectSingleResult().Data!["test"]);
        }

        [Fact]
        public void ParsesVariableWithMinutes()
        {
            var result = _testExecutor
                .Execute(OperationRequestBuilder.New()
                    .SetDocument("mutation($arg: OffsetTime!) { test(arg: $arg) }")
                    .SetVariableValues(new Dictionary<string, object?> { {"arg", "18:30:13+02:35" }, })
                    .Build());
            Assert.Equal("18:30:13+02:35", result.ExpectSingleResult().Data!["test"]);
        }

        [Fact]
        public void DoesntParseAnIncorrectVariable()
        {
            var result = _testExecutor
                .Execute(OperationRequestBuilder.New()
                    .SetDocument("mutation($arg: OffsetTime!) { test(arg: $arg) }")
                    .SetVariableValues(new Dictionary<string, object?> { {"arg", "18:30:13" }, })
                    .Build());
            Assert.Null(result.ExpectSingleResult().Data);
            Assert.Single(result.ExpectSingleResult().Errors!);
        }

        [Fact]
        public void ParsesLiteral()
        {
            var result = _testExecutor
                .Execute(OperationRequestBuilder.New()
                    .SetDocument("mutation { test(arg: \"18:30:13+02\") }")
                    .Build());
            Assert.Equal("18:30:13+02", result.ExpectSingleResult().Data!["test"]);
        }

        [Fact]
        public void ParsesLiteralWithMinutes()
        {
            var result = _testExecutor
                .Execute(OperationRequestBuilder.New()
                    .SetDocument("mutation { test(arg: \"18:30:13+02:35\") }")
                    .Build());
            Assert.Equal("18:30:13+02:35", result.ExpectSingleResult().Data!["test"]);
        }

        [Fact]
        public void DoesntParseIncorrectLiteral()
        {
            var result = _testExecutor
                .Execute(OperationRequestBuilder.New()
                    .SetDocument("mutation { test(arg: \"18:30:13\") }")
                    .Build());

            Assert.Null(result.ExpectSingleResult().Data);
            Assert.Single(result.ExpectSingleResult().Errors!);
            Assert.Null(result.ExpectSingleResult().Errors![0].Code);
            Assert.Equal(
                "Unable to deserialize string to OffsetTime",
                result.ExpectSingleResult().Errors![0].Message);
        }

        [Fact]
        public void PatternEmptyThrowSchemaException()
        {
            static object Call() => new OffsetTimeType([]);
            Assert.Throws<SchemaException>(Call);
        }
    }
}
