using System;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using Xunit;

namespace HotChocolate.Types.NodaTime.Tests
{
    public class IsoDayOfWeekTypeIntegrationTests
    {
        public static class Schema
        {
            public class Query
            {
                public IsoDayOfWeek Monday => IsoDayOfWeek.Monday;
                public IsoDayOfWeek Sunday => IsoDayOfWeek.Sunday;
                public IsoDayOfWeek Friday => IsoDayOfWeek.Friday;
                public IsoDayOfWeek None => IsoDayOfWeek.None;
            }

            public class Mutation
            {
                public IsoDayOfWeek Test(IsoDayOfWeek arg)
                {
                    var intRepr = (int)arg;
                    var nextIntRepr = Math.Max(1, (intRepr + 1) % 8);
                    return (IsoDayOfWeek)nextIntRepr;
                }
            }
        }

        private readonly IRequestExecutor testExecutor;
        public IsoDayOfWeekTypeIntegrationTests()
        {
            testExecutor = SchemaBuilder.New()
                .AddQueryType<Schema.Query>()
                .AddMutationType<Schema.Mutation>()
                .AddNodaTime()
                .Create()
                .MakeExecutable();
        }

        [Fact]
        public void QueryReturnsMonday()
        {
            IExecutionResult? result = testExecutor.Execute("query { test: monday }");
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal(1, queryResult!.Data!["test"]);
        }

        [Fact]
        public void QueryReturnsSunday()
        {
            IExecutionResult? result = testExecutor.Execute("query { test: sunday }");
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal(7, queryResult!.Data!["test"]);
        }

        [Fact]
        public void QueryReturnsFriday()
        {
            IExecutionResult? result = testExecutor.Execute("query { test: friday }");
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal(5, queryResult!.Data!["test"]);
        }

        [Fact]
        public void QueryDoesntReturnNone()
        {
            IExecutionResult? result = testExecutor.Execute("query { test: none }");
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Null(queryResult!.Data);
            Assert.NotEmpty(queryResult!.Errors);
        }

        [Fact]
        public void MutationParsesMonday()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: IsoDayOfWeek!) { test(arg: $arg) }")
                    .SetVariableValue("arg", 1)
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal(2, queryResult!.Data!["test"]);
        }

        [Fact]
        public void MutationParsesSunday()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: IsoDayOfWeek!) { test(arg: $arg) }")
                    .SetVariableValue("arg", 7)
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal(1, queryResult!.Data!["test"]);
        }

        [Fact]
        public void MutationDoesntParseZero()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: IsoDayOfWeek!) { test(arg: $arg) }")
                    .SetVariableValue("arg", 0)
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Null(queryResult!.Data);
            Assert.Equal(1, queryResult!.Errors!.Count);
        }

        [Fact]
        public void MutationDoesntParseEight()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: IsoDayOfWeek!) { test(arg: $arg) }")
                    .SetVariableValue("arg", 8)
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Null(queryResult!.Data);
            Assert.Equal(1, queryResult!.Errors!.Count);
        }

        [Fact]
        public void MutationDoesntParseNegativeNumbers()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: IsoDayOfWeek!) { test(arg: $arg) }")
                    .SetVariableValue("arg", -2)
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Null(queryResult!.Data);
            Assert.Equal(1, queryResult!.Errors!.Count);
        }
    }
}
