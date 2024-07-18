using System;
using System.Collections.Generic;
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
            var result = _testExecutor.Execute("query { test: rome }");
            Assert.Equal(
                "2020-12-31T18:30:13 Asia/Kathmandu +05:45",
                Assert.IsType<OperationResult>(result).Data!["test"]);
        }

        [Fact]
        public void QueryReturnsUtc()
        {
            var result = _testExecutor.Execute("query { test: utc }");
            Assert.Equal(
                "2020-12-31T18:30:13 UTC +00",
                Assert.IsType<OperationResult>(result).Data!["test"]);
        }

        [Fact]
        public void ParsesVariable()
        {
            var result = _testExecutor
                .Execute(
                    OperationRequestBuilder.New()
                        .SetDocument("mutation($arg: ZonedDateTime!) { test(arg: $arg) }")
                        .SetVariableValues(
                            new Dictionary<string, object?>
                            {
                                { "arg", "2020-12-31T19:30:13 Asia/Kathmandu +05:45" },
                            })
                        .Build());
            Assert.Equal(
                "2020-12-31T19:40:13 Asia/Kathmandu +05:45",
                Assert.IsType<OperationResult>(result).Data!["test"]);
        }

        [Fact]
        public void ParsesVariableWithUtc()
        {
            var result = _testExecutor
                .Execute(
                    OperationRequestBuilder.New()
                        .SetDocument("mutation($arg: ZonedDateTime!) { test(arg: $arg) }")
                        .SetVariableValues(
                            new Dictionary<string, object?>
                            {
                                { "arg", "2020-12-31T19:30:13 UTC +00" }
                            })
                        .Build());
            Assert.Equal(
                "2020-12-31T19:40:13 UTC +00",
                Assert.IsType<OperationResult>(result).Data!["test"]);
        }

        [Fact]
        public void DoesntParseAnIncorrectVariable()
        {
            var result = _testExecutor
                .Execute(
                    OperationRequestBuilder.New()
                        .SetDocument("mutation($arg: ZonedDateTime!) { test(arg: $arg) }")
                        .SetVariableValues(
                            new Dictionary<string, object?>
                            {
                                { "arg", "2020-12-31T19:30:13 UTC" },
                            })
                        .Build());
            Assert.Null(result.ExpectQueryResult().Data);
            Assert.Single(result.ExpectQueryResult().Errors!);
        }

        [Fact]
        public void ParsesLiteral()
        {
            var result = _testExecutor
                .Execute(
                    OperationRequestBuilder.New()
                        .SetDocument("mutation{test(arg:\"2020-12-31T19:30:13 Asia/Kathmandu +05:45\")}")
                        .Build());
            Assert.Equal(
                "2020-12-31T19:40:13 Asia/Kathmandu +05:45",
                result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void ParsesLiteralWithUtc()
        {
            var result = _testExecutor
                .Execute(
                    OperationRequestBuilder.New()
                        .SetDocument("mutation { test(arg: \"2020-12-31T19:30:13 UTC +00\") }")
                        .Build());
            Assert.Equal("2020-12-31T19:40:13 UTC +00", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void DoesntParseIncorrectLiteral()
        {
            var result = _testExecutor
                .Execute(
                    OperationRequestBuilder.New()
                        .SetDocument("mutation { test(arg: \"2020-12-31T19:30:13 UTC\") }")
                        .Build());
            Assert.Null(result.ExpectQueryResult().Data);
            Assert.Single(result.ExpectQueryResult().Errors!);
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
