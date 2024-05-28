using System;
using System.Collections.Generic;
using HotChocolate.Execution;
using NodaTime;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime.Tests
{
    public class OffsetDateTimeTypeIntegrationTests
    {
        public static class Schema
        {
            public class Query
            {
                public OffsetDateTime Hours
                    => OffsetDateTime
                        .FromDateTimeOffset(
                            new DateTimeOffset(
                                2020,
                                12,
                                31,
                                18,
                                30,
                                13,
                                TimeSpan.FromHours(2)))
                        .PlusNanoseconds(1234);

                public OffsetDateTime HoursAndMinutes
                    => OffsetDateTime
                        .FromDateTimeOffset(
                            new DateTimeOffset(
                                2020,
                                12,
                                31,
                                18,
                                30,
                                13,
                                TimeSpan.FromHours(2) + TimeSpan.FromMinutes(30)))
                        .PlusNanoseconds(1234);
            }

            public class Mutation
            {
                public OffsetDateTime Test(OffsetDateTime arg)
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
            var result = _testExecutor.Execute("query { test: hours }");

            Assert.Equal("2020-12-31T18:30:13+02", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void QueryReturnsWithMinutes()
        {
            var result = _testExecutor.Execute("query { test: hoursAndMinutes }");

            Assert.Equal("2020-12-31T18:30:13+02:30", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void ParsesVariable()
        {
            var result = _testExecutor
                .Execute(
                    OperationRequestBuilder.Create()
                        .SetDocument("mutation($arg: OffsetDateTime!) { test(arg: $arg) }")
                        .SetVariableValues(new Dictionary<string, object?> { { "arg", "2020-12-31T18:30:13+02" }, })
                        .Build());

            Assert.Equal("2020-12-31T18:40:13+02", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void ParsesVariableWithMinutes()
        {
            var result = _testExecutor
                .Execute(
                    OperationRequestBuilder.Create()
                        .SetDocument("mutation($arg: OffsetDateTime!) { test(arg: $arg) }")
                        .SetVariableValues(new Dictionary<string, object?> { { "arg", "2020-12-31T18:30:13+02:35" }, })
                        .Build());

            Assert.Equal("2020-12-31T18:40:13+02:35", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void DoesntParseAnIncorrectVariable()
        {
            var result = _testExecutor
                .Execute(
                    OperationRequestBuilder.Create()
                        .SetDocument("mutation($arg: OffsetDateTime!) { test(arg: $arg) }")
                        .SetVariableValues(new Dictionary<string, object?> { { "arg", "2020-12-31T18:30:13" }, })
                        .Build());

            Assert.Null(result.ExpectQueryResult().Data);
            Assert.Equal(1, result.ExpectQueryResult().Errors!.Count);
        }

        [Fact]
        public void ParsesLiteral()
        {
            var result = _testExecutor
                .Execute(
                    OperationRequestBuilder.Create()
                        .SetDocument("mutation { test(arg: \"2020-12-31T18:30:13+02\") }")
                        .Build());

            Assert.Equal("2020-12-31T18:40:13+02", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void ParsesLiteralWithMinutes()
        {
            var result = _testExecutor
                .Execute(
                    OperationRequestBuilder.Create()
                        .SetDocument("mutation { test(arg: \"2020-12-31T18:30:13+02:35\") }")
                        .Build());

            Assert.Equal("2020-12-31T18:40:13+02:35", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void DoesntParseIncorrectLiteral()
        {
            var result = _testExecutor
                .Execute(
                    OperationRequestBuilder.Create()
                        .SetDocument("mutation { test(arg: \"2020-12-31T18:30:13\") }")
                        .Build());

            Assert.Null(result.ExpectQueryResult().Data);
            Assert.Equal(1, result.ExpectQueryResult().Errors!.Count);
            Assert.Null(result.ExpectQueryResult().Errors![0].Code);
            Assert.Equal(
                "Unable to deserialize string to OffsetDateTime",
                result.ExpectQueryResult().Errors![0].Message);
        }

        [Fact]
        public void PatternEmptyThrowSchemaException()
        {
            static object Call() => new OffsetDateTimeType(Array.Empty<IPattern<OffsetDateTime>>());
            Assert.Throws<SchemaException>(Call);
        }
    }
}
