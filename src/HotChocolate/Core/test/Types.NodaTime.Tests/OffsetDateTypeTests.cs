using System;
using HotChocolate.Execution;
using NodaTime;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime.Tests
{
    public class OffsetDateTypeIntegrationTests
    {
        public static class Schema
        {
            public class Query
            {
                public OffsetDate Hours 
                    => new OffsetDate(
                        LocalDate.FromDateTime(new DateTime(2020, 12, 31, 18, 30, 13)),
                        Offset.FromHours(2)).WithCalendar(CalendarSystem.Gregorian);

                public OffsetDate HoursAndMinutes
                    => new OffsetDate(
                        LocalDate.FromDateTime(new DateTime(2020, 12, 31, 18, 30, 13)),
                        Offset.FromHoursAndMinutes(2, 35)).WithCalendar(CalendarSystem.Gregorian);
            }

            public class Mutation
            {
                public OffsetDate Test(OffsetDate arg) => arg;
            }
        }

        private readonly IRequestExecutor _testExecutor;

        public OffsetDateTypeIntegrationTests()
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
            IExecutionResult result = _testExecutor.Execute("query { test: hours }");
            Assert.Equal("2020-12-31+02", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void QueryReturnsWithMinutes()
        {
            IExecutionResult result = _testExecutor.Execute("query { test: hoursAndMinutes }");
            Assert.Equal("2020-12-31+02:35", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void ParsesVariable()
        {
            IExecutionResult result = _testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: OffsetDate!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "2020-12-31+02")
                    .Create());
            Assert.Equal("2020-12-31+02", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void ParsesVariableWithMinutes()
        {
            IExecutionResult result = _testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: OffsetDate!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "2020-12-31+02:35")
                    .Create());
            Assert.Equal("2020-12-31+02:35", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void DoesntParseAnIncorrectVariable()
        {
            IExecutionResult result = _testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: OffsetDate!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "2020-12-31")
                    .Create());
            Assert.Null(result.ExpectQueryResult().Data);
            Assert.Equal(1, result.ExpectQueryResult().Errors!.Count);
        }

        [Fact]
        public void ParsesLiteral()
        {
            IExecutionResult result = _testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"2020-12-31+02\") }")
                    .Create());
            Assert.Equal("2020-12-31+02", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void ParsesLiteralWithMinutes()
        {
            IExecutionResult result = _testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"2020-12-31+02:35\") }")
                    .Create());
            Assert.Equal("2020-12-31+02:35", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void DoesntParseIncorrectLiteral()
        {
            IExecutionResult result = _testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"2020-12-31\") }")
                    .Create());
            Assert.Null(result.ExpectQueryResult().Data);
            Assert.Equal(1, result.ExpectQueryResult().Errors!.Count);
            Assert.Null(result.ExpectQueryResult().Errors![0].Code);
            Assert.Equal(
                "Unable to deserialize string to OffsetDate",
                result.ExpectQueryResult().Errors![0].Message);
        }

        [Fact]
        public void PatternEmpty_ThrowSchemaException()
        {
            static object Call() => new OffsetDateType(Array.Empty<IPattern<OffsetDate>>());
            Assert.Throws<SchemaException>(Call);
        }
    }
}
