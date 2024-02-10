using System;
using HotChocolate.Execution;
using NodaTime;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime.Tests
{
    public class ZonedDateTimeTypeIntegrationTests
    {
        public static class Schema
        {
            public class Query
            {
                public ZonedDateTime Rome =>
                    new ZonedDateTime(
                        LocalDateTime.FromDateTime(new DateTime(2020, 12, 31, 18, 30, 13)),
                        DateTimeZoneProviders.Tzdb["Asia/Kathmandu"],
                        Offset.FromHoursAndMinutes(5, 45));

                public ZonedDateTime Utc =>
                    new ZonedDateTime(
                        LocalDateTime.FromDateTime(new DateTime(2020, 12, 31, 18, 30, 13)),
                        DateTimeZoneProviders.Tzdb["UTC"],
                        Offset.FromHours(0));
            }

            public class Mutation
            {
                public ZonedDateTime Test(ZonedDateTime arg)
                {
                    return arg + Duration.FromMinutes(10);
                }
            }
        }

        private readonly IRequestExecutor testExecutor;

        public ZonedDateTimeTypeIntegrationTests()
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
            IExecutionResult result = testExecutor.Execute("query { test: rome }");
            Assert.Equal(
                "2020-12-31T18:30:13 Asia/Kathmandu +05:45",
                Assert.IsType<QueryResult>(result).Data!["test"]);
        }

        [Fact]
        public void QueryReturnsUtc()
        {
            IExecutionResult result = testExecutor.Execute("query { test: utc }");
            Assert.Equal(
                "2020-12-31T18:30:13 UTC +00",
                Assert.IsType<QueryResult>(result).Data!["test"]);
        }

        [Fact]
        public void ParsesVariable()
        {
            IExecutionResult result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: ZonedDateTime!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "2020-12-31T19:30:13 Asia/Kathmandu +05:45")
                    .Create());
            Assert.Equal(
                "2020-12-31T19:40:13 Asia/Kathmandu +05:45",
                Assert.IsType<QueryResult>(result).Data!["test"]);
        }

        [Fact]
        public void ParsesVariableWithUTC()
        {
            IExecutionResult result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: ZonedDateTime!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "2020-12-31T19:30:13 UTC +00")
                    .Create());
            Assert.Equal(
                "2020-12-31T19:40:13 UTC +00",
                Assert.IsType<QueryResult>(result).Data!["test"]);
        }

        [Fact]
        public void DoesntParseAnIncorrectVariable()
        {
            IExecutionResult result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: ZonedDateTime!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "2020-12-31T19:30:13 UTC")
                    .Create());
            Assert.Null(result.ExpectQueryResult().Data);
            Assert.Equal(1, result.ExpectQueryResult().Errors!.Count);
        }

        [Fact]
        public void ParsesLiteral()
        {
            IExecutionResult result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation{test(arg:\"2020-12-31T19:30:13 Asia/Kathmandu +05:45\")}")
                    .Create());
            Assert.Equal(
                "2020-12-31T19:40:13 Asia/Kathmandu +05:45",
                result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void ParsesLiteralWithUTC()
        {
            IExecutionResult result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"2020-12-31T19:30:13 UTC +00\") }")
                    .Create());
            Assert.Equal("2020-12-31T19:40:13 UTC +00", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void DoesntParseIncorrectLiteral()
        {
            IExecutionResult result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"2020-12-31T19:30:13 UTC\") }")
                    .Create());
            Assert.Null(result.ExpectQueryResult().Data);
            Assert.Equal(1, result.ExpectQueryResult().Errors!.Count);
            Assert.Null(result.ExpectQueryResult().Errors![0].Code);
            Assert.Equal(
                "Unable to deserialize string to ZonedDateTime",
                result.ExpectQueryResult().Errors![0].Message);
        }

        [Fact]
        public void PatternEmpty_ThrowSchemaException()
        {
            static object Call() => new ZonedDateTimeType(Array.Empty<IPattern<ZonedDateTime>>());
            Assert.Throws<SchemaException>(Call);
        }
    }
}
