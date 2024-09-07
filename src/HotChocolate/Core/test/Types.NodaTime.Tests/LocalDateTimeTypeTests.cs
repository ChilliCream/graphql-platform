using HotChocolate.Execution;
using NodaTime;

namespace HotChocolate.Types.NodaTime.Tests
{
    public class LocalDateTimeTypeIntegrationTests
    {
        public static class Schema
        {
            public class Query
            {
                public LocalDateTime One =>
                    LocalDateTime.FromDateTime(
                            new DateTime(2020, 02, 20, 17, 42, 59))
                        .PlusNanoseconds(1234)
                        .WithCalendar(CalendarSystem.Julian);
            }

            public class Mutation
            {
                public LocalDateTime Test(LocalDateTime arg)
                {
                    return arg + Period.FromMinutes(10);
                }
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

            Assert.Equal("2020-02-07T17:42:59.000001234", result.ExpectSingleResult().Data!["test"]);
        }

        [Fact]
        public void ParsesVariable()
        {
            var result = _testExecutor
                .Execute(
                    OperationRequestBuilder.New()
                        .SetDocument("mutation($arg: LocalDateTime!) { test(arg: $arg) }")
                        .SetVariableValues(
                            new Dictionary<string, object?> { { "arg", "2020-02-21T17:42:59.000001234" }, })
                        .Build());

            Assert.Equal("2020-02-21T17:52:59.000001234", result.ExpectSingleResult().Data!["test"]);
        }

        [Fact]
        public void DoesntParseAnIncorrectVariable()
        {
            var result = _testExecutor
                .Execute(
                    OperationRequestBuilder.New()
                        .SetDocument("mutation($arg: LocalDateTime!) { test(arg: $arg) }")
                        .SetVariableValues(
                            new Dictionary<string, object?> { { "arg", "2020-02-20T17:42:59.000001234Z" }, })
                        .Build());

            Assert.Null(result.ExpectSingleResult().Data);
            Assert.Single(result.ExpectSingleResult().Errors!);
        }

        [Fact]
        public void ParsesLiteral()
        {
            var result = _testExecutor
                .Execute(
                    OperationRequestBuilder.New()
                        .SetDocument("mutation { test(arg: \"2020-02-20T17:42:59.000001234\") }")
                        .Build());

            Assert.Equal("2020-02-20T17:52:59.000001234", result.ExpectSingleResult().Data!["test"]);
        }

        [Fact]
        public void DoesntParseIncorrectLiteral()
        {
            var result = _testExecutor
                .Execute(
                    OperationRequestBuilder.New()
                        .SetDocument("mutation { test(arg: \"2020-02-20T17:42:59.000001234Z\") }")
                        .Build());

            Assert.Null(result.ExpectSingleResult().Data);
            Assert.Single(result.ExpectSingleResult().Errors!);
            Assert.Null(result.ExpectSingleResult().Errors![0].Code);
            Assert.Equal(
                "Unable to deserialize string to LocalDateTime",
                result.ExpectSingleResult().Errors![0].Message);
        }

        [Fact]
        public void PatternEmpty_ThrowSchemaException()
        {
            static object Call() => new LocalDateTimeType([]);
            Assert.Throws<SchemaException>(Call);
        }
    }
}
