using System;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using NodaTime.Text;
using Xunit;

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
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("+02", queryResult!.Data!["test"]);
        }

        [Fact]
        public void QueryReturnsWithMinutes()
        {
            IExecutionResult? result = testExecutor.Execute("query { test: hoursAndMinutes }");
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("+02:35", queryResult!.Data!["test"]);
        }

        [Fact]
        public void QueryReturnsWithZ()
        {
            IExecutionResult? result = testExecutor.Execute("query { test: zOffset }");
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("Z", queryResult!.Data!["test"]);
        }

        [Fact]
        public void ParsesVariable()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: Offset!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "+02")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("+03:05", queryResult!.Data!["test"]);
        }

        [Fact]
        public void ParsesVariableWithMinutes()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: Offset!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "+02:35")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("+03:40", queryResult!.Data!["test"]);
        }

        [Fact]
        public void DoesntParseAnIncorrectVariable()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: Offset!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "18:30:13+02")
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
                    .SetQuery("mutation { test(arg: \"+02\") }")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("+03:05", queryResult!.Data!["test"]);
        }

        [Fact]
        public void ParsesLiteralWithMinutes()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"+02:35\") }")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("+03:40", queryResult!.Data!["test"]);
        }

        [Fact]
        public void ParsesLiteralWithZ()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"Z\") }")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("+01:05", queryResult!.Data!["test"]);
        }

        [Fact]
        public void DoesntParseIncorrectLiteral()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"18:30:13+02\") }")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Null(queryResult!.Data);
            Assert.Equal(1, queryResult!.Errors!.Count);
            Assert.Null(queryResult.Errors[0].Code);
            Assert.Equal("Unable to deserialize string to Offset", queryResult.Errors[0].Message);
        }

        [Fact]
        public void PatternEmpty_ThrowSchemaException()
        {
            static object Call() => new OffsetType(Array.Empty<IPattern<Offset>>());
            Assert.Throws<SchemaException>(Call);
        }
    }
}
