using System;
using HotChocolate.Execution;
using NodaTime;
using NodaTime.Text;

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

            Assert.Equal("12:42:13.031011234", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void ParsesVariable()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: LocalTime!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "12:42:13.031011234")
                    .Create());

            Assert.Equal("12:52:13.031011234", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void ParsesVariableWithoutTicks()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: LocalTime!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "12:42:13")
                    .Create());

            Assert.Equal("12:52:13", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void DoesntParseAnIncorrectVariable()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: LocalTime!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "12:42")
                    .Create());

            Assert.Null(result.ExpectQueryResult().Data);
            Assert.Equal(1, result.ExpectQueryResult().Errors!.Count);
        }

        [Fact]
        public void ParsesLiteral()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"12:42:13.031011234\") }")
                    .Create());

            Assert.Equal("12:52:13.031011234", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void ParsesLiteralWithoutTick()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"12:42:13\") }")
                    .Create());

            Assert.Equal("12:52:13", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void DoesntParseIncorrectLiteral()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"12:42\") }")
                    .Create());

            Assert.Null(result.ExpectQueryResult().Data);
            Assert.Equal(1, result.ExpectQueryResult().Errors!.Count);
            Assert.Null(result.ExpectQueryResult().Errors![0].Code);
            Assert.Equal(
                "Unable to deserialize string to LocalTime",
                result.ExpectQueryResult().Errors![0].Message);
        }

        [Fact]
        public void PatternEmpty_ThrowSchemaException()
        {
            static object Call() => new LocalTimeType(Array.Empty<IPattern<LocalTime>>());
            Assert.Throws<SchemaException>(Call);
        }
    }
}
