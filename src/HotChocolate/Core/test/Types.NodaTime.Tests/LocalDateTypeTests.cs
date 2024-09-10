using HotChocolate.Execution;
using NodaTime;

namespace HotChocolate.Types.NodaTime.Tests
{
    public class LocalDateTypeIntegrationTests
    {
        public static class Schema
        {
            public class Query
            {
                public LocalDate One => LocalDate.FromDateTime(
                    new DateTime(2020, 02, 20, 17, 42, 59))
                        .WithCalendar(CalendarSystem.HebrewCivil);
            }

            public class Mutation
            {
                public LocalDate Test(LocalDate arg)
                {
                    return arg + Period.FromDays(3);
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

            Assert.Equal("5780-05-25", result.ExpectSingleResult().Data!["test"]);
        }

        [Fact]
        public void ParsesVariable()
        {
            var result = _testExecutor
                .Execute(OperationRequestBuilder.New()
                    .SetDocument("mutation($arg: LocalDate!) { test(arg: $arg) }")
                    .SetVariableValues(new Dictionary<string, object?> { {"arg", "2020-02-21" }, })
                    .Build());

            Assert.Equal("2020-02-24", result.ExpectSingleResult().Data!["test"]);
        }

        [Fact]
        public void DoesntParseAnIncorrectVariable()
        {
            var result = _testExecutor
                .Execute(OperationRequestBuilder.New()
                    .SetDocument("mutation($arg: LocalDate!) { test(arg: $arg) }")
                    .SetVariableValues(new Dictionary<string, object?> { {"arg", "2020-02-20T17:42:59" }, })
                    .Build());

            Assert.Null(result.ExpectSingleResult().Data);
            Assert.Single(result.ExpectSingleResult().Errors!);
        }

        [Fact]
        public void ParsesLiteral()
        {
            var result = _testExecutor
                .Execute(OperationRequestBuilder.New()
                    .SetDocument("mutation { test(arg: \"2020-02-20\") }")
                    .Build());

            Assert.Equal("2020-02-23", result.ExpectSingleResult().Data!["test"]);
        }

        [Fact]
        public void DoesntParseIncorrectLiteral()
        {
            var result = _testExecutor
                .Execute(OperationRequestBuilder.New()
                    .SetDocument("mutation { test(arg: \"2020-02-20T17:42:59\") }")
                    .Build());

            Assert.Null(result.ExpectSingleResult().Data);
            Assert.Single(result.ExpectSingleResult().Errors!);
            Assert.Null(result.ExpectSingleResult().Errors![0].Code);
            Assert.Equal(
                "Unable to deserialize string to LocalDate",
                result.ExpectSingleResult().Errors![0].Message);
        }

        [Fact]
        public void PatternEmpty_ThrowSchemaException()
        {
            static object Call() => new LocalDateType([]);
            Assert.Throws<SchemaException>(Call);
        }
    }
}
