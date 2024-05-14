using System;
using HotChocolate.Execution;
using NodaTime;

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
            var result = testExecutor.Execute("query { test: monday }");
            
            Assert.Equal(1, result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void QueryReturnsSunday()
        {
            var result = testExecutor.Execute("query { test: sunday }");
            
            Assert.Equal(7, result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void QueryReturnsFriday()
        {
            var result = testExecutor.Execute("query { test: friday }");
            
            Assert.Equal(5, result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void QueryDoesntReturnNone()
        {
            var result = testExecutor.Execute("query { test: none }");
            
            Assert.Null(result.ExpectQueryResult().Data);
            Assert.NotEmpty(result.ExpectQueryResult().Errors);
        }

        [Fact]
        public void MutationParsesMonday()
        {
            var result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: IsoDayOfWeek!) { test(arg: $arg) }")
                    .SetVariableValue("arg", 1)
                    .Create());
            
            Assert.Equal(2, result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void MutationParsesSunday()
        {
            var result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: IsoDayOfWeek!) { test(arg: $arg) }")
                    .SetVariableValue("arg", 7)
                    .Create());
            
            Assert.Equal(1, result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void MutationDoesntParseZero()
        {
            var result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: IsoDayOfWeek!) { test(arg: $arg) }")
                    .SetVariableValue("arg", 0)
                    .Create());
            
            Assert.Null(result.ExpectQueryResult().Data);
            Assert.Equal(1, result.ExpectQueryResult().Errors!.Count);
        }

        [Fact]
        public void MutationDoesntParseEight()
        {
            var result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: IsoDayOfWeek!) { test(arg: $arg) }")
                    .SetVariableValue("arg", 8)
                    .Create());
            
            Assert.Null(result.ExpectQueryResult().Data);
            Assert.Equal(1, result.ExpectQueryResult().Errors!.Count);
        }

        [Fact]
        public void MutationDoesntParseNegativeNumbers()
        {
            var result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: IsoDayOfWeek!) { test(arg: $arg) }")
                    .SetVariableValue("arg", -2)
                    .Create());
            
            Assert.Null(result.ExpectQueryResult().Data);
            Assert.Equal(1, result.ExpectQueryResult().Errors!.Count);
        }
    }
}
