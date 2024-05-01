using System;
using System.Collections.Generic;
using HotChocolate.Execution;
using NodaTime;
using NodaTime.Text;

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

            Assert.Equal("2020-02-07T17:42:59.000001234", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void ParsesVariable()
        {
            var result = _testExecutor
                .Execute(
                    OperationRequestBuilder.Create()
                        .SetDocument("mutation($arg: LocalDateTime!) { test(arg: $arg) }")
                        .SetVariableValues(
                            new Dictionary<string, object?> { { "arg", "2020-02-21T17:42:59.000001234" }, })
                        .Build());

            Assert.Equal("2020-02-21T17:52:59.000001234", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void DoesntParseAnIncorrectVariable()
        {
            var result = _testExecutor
                .Execute(
                    OperationRequestBuilder.Create()
                        .SetDocument("mutation($arg: LocalDateTime!) { test(arg: $arg) }")
                        .SetVariableValues(
                            new Dictionary<string, object?> { { "arg", "2020-02-20T17:42:59.000001234Z" }, })
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
                        .SetDocument("mutation { test(arg: \"2020-02-20T17:42:59.000001234\") }")
                        .Build());

            Assert.Equal("2020-02-20T17:52:59.000001234", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void DoesntParseIncorrectLiteral()
        {
            var result = _testExecutor
                .Execute(
                    OperationRequestBuilder.Create()
                        .SetDocument("mutation { test(arg: \"2020-02-20T17:42:59.000001234Z\") }")
                        .Build());

            Assert.Null(result.ExpectQueryResult().Data);
            Assert.Equal(1, result.ExpectQueryResult().Errors!.Count);
            Assert.Null(result.ExpectQueryResult().Errors![0].Code);
            Assert.Equal(
                "Unable to deserialize string to LocalDateTime",
                result.ExpectQueryResult().Errors![0].Message);
        }

        [Fact]
        public void PatternEmpty_ThrowSchemaException()
        {
            static object Call() => new LocalDateTimeType(Array.Empty<IPattern<LocalDateTime>>());
            Assert.Throws<SchemaException>(Call);
        }
    }
}
