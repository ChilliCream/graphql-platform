using System;
using System.Collections.Generic;
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

        private readonly IRequestExecutor _testExecutor =
            SchemaBuilder.New()
                .AddQueryType<Schema.Query>()
                .AddMutationType<Schema.Mutation>()
                .AddNodaTime()
                .Create()
                .MakeExecutable();

        [Fact]
        public void QueryReturnsMonday()
        {
            var result = _testExecutor.Execute("query { test: monday }");

            Assert.Equal(1, result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void QueryReturnsSunday()
        {
            var result = _testExecutor.Execute("query { test: sunday }");

            Assert.Equal(7, result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void QueryReturnsFriday()
        {
            var result = _testExecutor.Execute("query { test: friday }");

            Assert.Equal(5, result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void QueryDoesntReturnNone()
        {
            var result = _testExecutor.Execute("query { test: none }");

            Assert.Null(result.ExpectQueryResult().Data);
            Assert.NotEmpty(result.ExpectQueryResult().Errors);
        }

        [Fact]
        public void MutationParsesMonday()
        {
            var result = _testExecutor
                .Execute(OperationRequestBuilder.Create()
                    .SetDocument("mutation($arg: IsoDayOfWeek!) { test(arg: $arg) }")
                    .SetVariableValues(new Dictionary<string, object?> { {"arg", 1 }, })
                    .Build());

            Assert.Equal(2, result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void MutationParsesSunday()
        {
            var result = _testExecutor
                .Execute(OperationRequestBuilder.Create()
                    .SetDocument("mutation($arg: IsoDayOfWeek!) { test(arg: $arg) }")
                    .SetVariableValues(new Dictionary<string, object?> { {"arg", 7 }, })
                    .Build());

            Assert.Equal(1, result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void MutationDoesntParseZero()
        {
            var result = _testExecutor
                .Execute(OperationRequestBuilder.Create()
                    .SetDocument("mutation($arg: IsoDayOfWeek!) { test(arg: $arg) }")
                    .SetVariableValues(new Dictionary<string, object?> { {"arg", 0 }, })
                    .Build());

            Assert.Null(result.ExpectQueryResult().Data);
            Assert.Equal(1, result.ExpectQueryResult().Errors!.Count);
        }

        [Fact]
        public void MutationDoesntParseEight()
        {
            var result = _testExecutor
                .Execute(OperationRequestBuilder.Create()
                    .SetDocument("mutation($arg: IsoDayOfWeek!) { test(arg: $arg) }")
                    .SetVariableValues(new Dictionary<string, object?> { {"arg", 8 }, })
                    .Build());

            Assert.Null(result.ExpectQueryResult().Data);
            Assert.Equal(1, result.ExpectQueryResult().Errors!.Count);
        }

        [Fact]
        public void MutationDoesntParseNegativeNumbers()
        {
            var result = _testExecutor
                .Execute(OperationRequestBuilder.Create()
                    .SetDocument("mutation($arg: IsoDayOfWeek!) { test(arg: $arg) }")
                    .SetVariableValues(new Dictionary<string, object?> { {"arg", -2 }, })
                    .Build());

            Assert.Null(result.ExpectQueryResult().Data);
            Assert.Equal(1, result.ExpectQueryResult().Errors!.Count);
        }
    }
}
