using System;
using HotChocolate.Execution;
using NodaTime;
using NodaTime.Text;
using Xunit;

namespace HotChocolate.Types.NodaTime.Tests
{
    public class DurationTypeIntegrationTests
    {
        public static class Schema
        {
            public class Query
            {
                public Duration PositiveWithDecimals
                    => Duration.FromTimeSpan(new TimeSpan(123, 7, 53, 10, 19));
                public Duration NegativeWithDecimals
                    => -Duration.FromTimeSpan(new TimeSpan(123, 7, 53, 10, 19));
                public Duration PositiveWithoutDecimals
                    => Duration.FromTimeSpan(new TimeSpan(123, 7, 53, 10));
                public Duration PositiveWithoutSeconds
                    => Duration.FromTimeSpan(new TimeSpan(123, 7, 53, 0));
                public Duration PositiveWithoutMinutes
                    => Duration.FromTimeSpan(new TimeSpan(123, 7, 0, 0));
                public Duration PositiveWithRoundtrip
                    => Duration.FromTimeSpan(new TimeSpan(123, 26, 0, 70));
            }

            public class Mutation
            {
                public Duration Test(Duration arg)
                    => arg + Duration.FromMinutes(10);
            }
        }

        private readonly IRequestExecutor _testExecutor;

        public DurationTypeIntegrationTests()
        {
            _testExecutor = SchemaBuilder.New()
                .AddQueryType<Schema.Query>()
                .AddMutationType<Schema.Mutation>()
                .AddNodaTime()
                .Create()
                .MakeExecutable();
        }

        [Fact]
        public void QueryReturnsSerializedDataWithDecimals()
        {
            IExecutionResult result = _testExecutor.Execute("query { test: positiveWithDecimals }");
            Assert.Equal("123:07:53:10.019", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void QueryReturnsSerializedDataWithNegativeValue()
        {
            IExecutionResult result = _testExecutor.Execute("query{test: negativeWithDecimals}");
            Assert.Equal("-123:07:53:10.019", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void QueryReturnsSerializedDataWithoutDecimals()
        {
            IExecutionResult result = _testExecutor.Execute("query{test: positiveWithoutDecimals}");
            Assert.Equal("123:07:53:10", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void QueryReturnsSerializedDataWithoutSeconds()
        {
            IExecutionResult result = _testExecutor.Execute("query{test:positiveWithoutSeconds}");
            Assert.Equal("123:07:53:00", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void QueryReturnsSerializedDataWithoutMinutes()
        {
            IExecutionResult result = _testExecutor.Execute("query{test:positiveWithoutMinutes}");
            Assert.Equal("123:07:00:00", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void QueryReturnsSerializedDataWithRoundtrip()
        {
            IExecutionResult result = _testExecutor.Execute("query{test:positiveWithRoundtrip}");
            Assert.Equal("124:02:01:10", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void MutationParsesInputWithDecimals()
        {
            IExecutionResult result = _testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: Duration!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "09:22:01:00.019")
                    .Create());
            Assert.Equal("9:22:11:00.019", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void MutationParsesInputWithoutDecimals()
        {
            IExecutionResult result = _testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: Duration!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "09:22:01:00")
                    .Create());
            Assert.Equal("9:22:11:00", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void MutationParsesInputWithoutLeadingZero()
        {
            IExecutionResult result = _testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: Duration!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "9:22:01:00")
                    .Create());
            Assert.Equal("9:22:11:00", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void MutationParsesInputWithNegativeValue()
        {
            IExecutionResult result = _testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: Duration!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "-9:22:01:00")
                    .Create());
            Assert.Equal("-9:21:51:00", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void MutationDoesntParseInputWithPlusSign()
        {
            IExecutionResult result = _testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: Duration!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "+09:22:01:00")
                    .Create());
            Assert.Null(result.ExpectQueryResult().Data);
            Assert.Equal(1, result.ExpectQueryResult().Errors!.Count);
        }

        [Fact]
        public void MutationDoesntParseInputWithOverflownHours()
        {
            IExecutionResult result = _testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: Duration!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "9:26:01:00")
                    .Create());
            Assert.Null(result.ExpectQueryResult().Data);
            Assert.Equal(1, result.ExpectQueryResult().Errors!.Count);
        }

        [Fact]
        public void MutationParsesLiteralWithDecimals()
        {
            IExecutionResult result = _testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"09:22:01:00.019\") }")
                    .Create());

            Assert.Equal("9:22:11:00.019", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void MutationParsesLiteralWithoutDecimals()
        {
            IExecutionResult result = _testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"09:22:01:00\") }")
                    .Create());

            Assert.Equal("9:22:11:00", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void MutationParsesLiteralWithoutLeadingZero()
        {
            IExecutionResult result = _testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"09:22:01:00\") }")
                    .Create());

            Assert.Equal("9:22:11:00", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void MutationParsesLiteralWithNegativeValue()
        {
            IExecutionResult result = _testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"-9:22:01:00\") }")
                    .Create());

            Assert.Equal("-9:21:51:00", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void MutationDoesntParseLiteralWithPlusSign()
        {
            IExecutionResult result = _testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"+09:22:01:00\") }")
                    .Create());

            Assert.Null(result.ExpectQueryResult().Data);
            Assert.Equal(1, result.ExpectQueryResult().Errors!.Count);
        }

        [Fact]
        public void MutationDoesntParseLiteralWithOverflownHours()
        {
            IExecutionResult result = _testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"9:26:01:00\") }")
                    .Create());

            Assert.Null(result.ExpectQueryResult().Data);
            Assert.Equal(1, result.ExpectQueryResult().Errors!.Count);
        }

        [Fact]
        public void PatternEmpty_ThrowSchemaException()
        {
            static object Call() => new DurationType(Array.Empty<IPattern<Duration>>());
            Assert.Throws<SchemaException>(Call);
        }
    }
}
