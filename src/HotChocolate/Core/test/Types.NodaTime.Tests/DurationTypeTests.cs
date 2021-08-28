using System;
using System.Linq;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using Xunit;

namespace HotChocolate.Types.NodaTime.Tests
{
    public class DurationTypeIntegrationTests
    {
        public static class Schema
        {
            public class Query
            {
                public Duration PositiveWithDecimals => Duration.FromTimeSpan(new TimeSpan(123, 7, 53, 10, 19));
                public Duration NegativeWithDecimals => -Duration.FromTimeSpan(new TimeSpan(123, 7, 53, 10, 19));
                public Duration PositiveWithoutDecimals => Duration.FromTimeSpan(new TimeSpan(123, 7, 53, 10));
                public Duration PositiveWithoutSeconds => Duration.FromTimeSpan(new TimeSpan(123, 7, 53, 0));
                public Duration PositiveWithoutMinutes => Duration.FromTimeSpan(new TimeSpan(123, 7, 0, 0));
                public Duration PositiveWithRoundtrip => Duration.FromTimeSpan(new TimeSpan(123, 26, 0, 70));
            }

            public class Mutation
            {
                public Duration Test(Duration arg)
                    => arg + Duration.FromMinutes(10);
            }
        }

        private readonly IRequestExecutor testExecutor;
        public DurationTypeIntegrationTests()
        {
            testExecutor = SchemaBuilder.New()
                .AddQueryType<Schema.Query>()
                .AddMutationType<Schema.Mutation>()
                .AddNodaTime()
                .Create()
                .MakeExecutable();
        }

        [Fact]
        public void QueryReturnsSerializedDataWithDecimals()
        {
            IExecutionResult? result = testExecutor.Execute("query { test: positiveWithDecimals }");
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("123:07:53:10.019", queryResult!.Data!["test"]);
        }

        [Fact]
        public void QueryReturnsSerializedDataWithNegativeValue()
        {
            IExecutionResult? result = testExecutor.Execute("query { test: negativeWithDecimals }");
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("-123:07:53:10.019", queryResult!.Data!["test"]);
        }

        [Fact]
        public void QueryReturnsSerializedDataWithoutDecimals()
        {
            IExecutionResult? result = testExecutor.Execute("query { test: positiveWithoutDecimals }");
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("123:07:53:10", queryResult!.Data!["test"]);
        }

        [Fact]
        public void QueryReturnsSerializedDataWithoutSeconds()
        {
            IExecutionResult? result = testExecutor.Execute("query { test: positiveWithoutSeconds }");
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("123:07:53:00", queryResult!.Data!["test"]);
        }

        [Fact]
        public void QueryReturnsSerializedDataWithoutMinutes()
        {
            IExecutionResult? result = testExecutor.Execute("query { test: positiveWithoutMinutes }");
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("123:07:00:00", queryResult!.Data!["test"]);
        }

        [Fact]
        public void QueryReturnsSerializedDataWithRoundtrip()
        {
            IExecutionResult? result = testExecutor.Execute("query { test: positiveWithRoundtrip }");
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("124:02:01:10", queryResult!.Data!["test"]);
        }

        [Fact]
        public void MutationParsesInputWithDecimals()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: Duration!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "09:22:01:00.019")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("9:22:11:00.019", queryResult!.Data!["test"]);
        }

        [Fact]
        public void MutationParsesInputWithoutDecimals()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: Duration!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "09:22:01:00")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("9:22:11:00", queryResult!.Data!["test"]);
        }

        [Fact]
        public void MutationParsesInputWithoutLeadingZero()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: Duration!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "9:22:01:00")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("9:22:11:00", queryResult!.Data!["test"]);
        }

        [Fact]
        public void MutationParsesInputWithNegativeValue()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: Duration!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "-9:22:01:00")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("-9:21:51:00", queryResult!.Data!["test"]);
        }

        [Fact]
        public void MutationDoesntParseInputWithPlusSign()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: Duration!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "+09:22:01:00")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Null(queryResult!.Data);
            Assert.Equal(1, queryResult!.Errors!.Count);
        }

        [Fact]
        public void MutationDoesntParseInputWithOverflownHours()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: Duration!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "9:26:01:00")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Null(queryResult!.Data);
            Assert.Equal(1, queryResult!.Errors!.Count);
        }

        [Fact]
        public void MutationParsesLiteralWithDecimals()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"09:22:01:00.019\") }")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("9:22:11:00.019", queryResult!.Data!["test"]);
        }

        [Fact]
        public void MutationParsesLiteralWithoutDecimals()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"09:22:01:00\") }")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("9:22:11:00", queryResult!.Data!["test"]);
        }

        [Fact]
        public void MutationParsesLiteralWithoutLeadingZero()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"09:22:01:00\") }")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("9:22:11:00", queryResult!.Data!["test"]);
        }

        [Fact]
        public void MutationParsesLiteralWithNegativeValue()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"-9:22:01:00\") }")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("-9:21:51:00", queryResult!.Data!["test"]);
        }

        [Fact]
        public void MutationDoesntParseLiteralWithPlusSign()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"+09:22:01:00\") }")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Null(queryResult!.Data);
            Assert.Equal(1, queryResult!.Errors!.Count);
        }

        [Fact]
        public void MutationDoesntParseLiteralWithOverflownHours()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"9:26:01:00\") }")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Null(queryResult!.Data);
            Assert.Equal(1, queryResult!.Errors!.Count);
        }
    }
}
