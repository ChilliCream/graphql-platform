using System;
using System.Linq;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using NodaTime.Text;
using Xunit;

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

        private readonly IRequestExecutor testExecutor;

        public LocalDateTimeTypeIntegrationTests()
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
            IExecutionResult? result = testExecutor.Execute("query { test: one }");
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("2020-02-07T17:42:59.000001234", queryResult!.Data!["test"]);
        }

        [Fact]
        public void ParsesVariable()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: LocalDateTime!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "2020-02-21T17:42:59.000001234")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("2020-02-21T17:52:59.000001234", queryResult!.Data!["test"]);
        }

        [Fact]
        public void DoesntParseAnIncorrectVariable()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: LocalDateTime!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "2020-02-20T17:42:59.000001234Z")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Null(queryResult!.Data);
            Assert.Equal(1, queryResult!.Errors!.Count);
        }

        [Fact]
        public void ParsesLiteral()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"2020-02-20T17:42:59.000001234\") }")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("2020-02-20T17:52:59.000001234", queryResult!.Data!["test"]);
        }

        [Fact]
        public void DoesntParseIncorrectLiteral()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"2020-02-20T17:42:59.000001234Z\") }")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Null(queryResult!.Data);
            Assert.Equal(1, queryResult!.Errors!.Count);
            Assert.Null(queryResult.Errors[0].Code);
            Assert.Equal(
                "Unable to deserialize string to LocalDateTime",
                queryResult.Errors[0].Message);
        }

        [Fact]
        public void PatternEmpty_ThrowSchemaException()
        {
            static object Call() => new LocalDateTimeType(Array.Empty<IPattern<LocalDateTime>>());
            Assert.Throws<SchemaException>(Call);
        }
    }
}
