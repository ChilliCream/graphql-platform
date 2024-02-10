using System;
using HotChocolate.Execution;
using NodaTime;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime.Tests
{
    public class OffsetTypeIntegrationTests
    {
        public static class Schema
        {
            public class Query
            {
                public Offset Hours => Offset.FromHours(2);
                public Offset HoursAndMinutes => Offset.FromHoursAndMinutes(2, 35);
                public Offset ZOffset => Offset.Zero;
            }

            public class Mutation
            {
                public Offset Test(Offset arg)
                    => arg + Offset.FromHoursAndMinutes(1, 5);
            }
        }

        private readonly IRequestExecutor testExecutor;

        public OffsetTypeIntegrationTests()
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
            IExecutionResult? result = testExecutor.Execute("query { test: hours }");
            Assert.Equal("+02", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void QueryReturnsWithMinutes()
        {
            IExecutionResult? result = testExecutor.Execute("query { test: hoursAndMinutes }");
            Assert.Equal("+02:35", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void QueryReturnsWithZ()
        {
            IExecutionResult? result = testExecutor.Execute("query { test: zOffset }");
            Assert.Equal("Z", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void ParsesVariable()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: Offset!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "+02")
                    .Create());
            Assert.Equal("+03:05", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void ParsesVariableWithMinutes()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: Offset!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "+02:35")
                    .Create());
            Assert.Equal("+03:40", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void DoesntParseAnIncorrectVariable()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: Offset!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "18:30:13+02")
                    .Create());
            Assert.Null(result.ExpectQueryResult().Data);
            Assert.Equal(1, result.ExpectQueryResult().Errors!.Count);
        }

        [Fact]
        public void ParsesLiteral()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"+02\") }")
                    .Create());
            Assert.Equal("+03:05", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void ParsesLiteralWithMinutes()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"+02:35\") }")
                    .Create());
            Assert.Equal("+03:40", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void ParsesLiteralWithZ()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"Z\") }")
                    .Create());
            Assert.Equal("+01:05", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void DoesntParseIncorrectLiteral()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"18:30:13+02\") }")
                    .Create());
            Assert.Null(result.ExpectQueryResult().Data);
            Assert.Equal(1, result.ExpectQueryResult().Errors!.Count);
            Assert.Null(result.ExpectQueryResult().Errors![0].Code);
            Assert.Equal(
                "Unable to deserialize string to Offset", 
                result.ExpectQueryResult().Errors![0].Message);
        }

        [Fact]
        public void PatternEmpty_ThrowSchemaException()
        {
            static object Call() => new OffsetType(Array.Empty<IPattern<Offset>>());
            Assert.Throws<SchemaException>(Call);
        }
    }
}
