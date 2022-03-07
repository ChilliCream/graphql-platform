using System;
using System.Linq;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using NodaTime.Text;
using Xunit;

namespace HotChocolate.Types.NodaTime.Tests
{
    public class LocalTimeTypeIntegrationTests
    {
        public static class Schema
        {
            public class Query
            {
                public LocalTime One => LocalTime
                    .FromHourMinuteSecondMillisecondTick(12, 42, 13, 31, 100)
                    .PlusNanoseconds(1234);
            }

            public class Mutation
            {
                public LocalTime Test(LocalTime arg)
                {
                    return arg + Period.FromMinutes(10);
                }
            }
        }

        private readonly IRequestExecutor testExecutor;

        public LocalTimeTypeIntegrationTests()
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
            Assert.Equal("12:42:13.031011234", queryResult!.Data!["test"]);
        }

        [Fact]
        public void ParsesVariable()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: LocalTime!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "12:42:13.031011234")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("12:52:13.031011234", queryResult!.Data!["test"]);
        }

        [Fact]
        public void ParsesVariableWithoutTicks()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: LocalTime!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "12:42:13")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("12:52:13", queryResult!.Data!["test"]);
        }

        [Fact]
        public void DoesntParseAnIncorrectVariable()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: LocalTime!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "12:42")
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
                    .SetQuery("mutation { test(arg: \"12:42:13.031011234\") }")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("12:52:13.031011234", queryResult!.Data!["test"]);
        }

        [Fact]
        public void ParsesLiteralWithoutTick()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"12:42:13\") }")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("12:52:13", queryResult!.Data!["test"]);
        }

        [Fact]
        public void DoesntParseIncorrectLiteral()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"12:42\") }")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Null(queryResult!.Data);
            Assert.Equal(1, queryResult!.Errors!.Count);
            Assert.Null(queryResult.Errors[0].Code);
            Assert.Equal(
                "Unable to deserialize string to LocalTime",
                queryResult.Errors[0].Message);
        }

        [Fact]
        public void PatternEmpty_ThrowSchemaException()
        {
            static object Call() => new LocalTimeType(Array.Empty<IPattern<LocalTime>>());
            Assert.Throws<SchemaException>(Call);
        }
    }
}
