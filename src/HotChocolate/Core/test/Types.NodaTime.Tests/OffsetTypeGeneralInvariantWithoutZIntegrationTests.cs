using HotChocolate.Execution;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime.Tests
{
    public class OffsetTypeGeneralInvariantWithoutZIntegrationTests
    {
        private readonly IRequestExecutor _testExecutor;

        public OffsetTypeGeneralInvariantWithoutZIntegrationTests()
        {
            _testExecutor = SchemaBuilder.New()
                .AddQueryType<OffsetTypeIntegrationTests.Schema.Query>()
                .AddMutationType<OffsetTypeIntegrationTests.Schema.Mutation>()
                .AddNodaTime(typeof(OffsetType))
                .AddType(new OffsetType(OffsetPattern.GeneralInvariant))
                .Create()
                .MakeExecutable();
        }

        [Fact]
        public void QueryReturns()
        {
            IExecutionResult? result = _testExecutor.Execute("query { test: hours }");
            Assert.Equal("+02", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void QueryReturnsWithMinutes()
        {
            IExecutionResult? result = _testExecutor.Execute("query { test: hoursAndMinutes }");
            Assert.Equal("+02:35", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void QueryReturnsWithZ()
        {
            IExecutionResult? result = _testExecutor.Execute("query { test: zOffset }");
            Assert.Equal("+00", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void ParsesVariable()
        {
            IExecutionResult? result = _testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: Offset!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "+02")
                    .Create());
            Assert.Equal("+03:05", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void ParsesVariableWithMinutes()
        {
            IExecutionResult? result = _testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: Offset!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "+02:35")
                    .Create());
            Assert.Equal("+03:40", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void DoesntParseAnIncorrectVariable()
        {
            IExecutionResult? result = _testExecutor
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
            IExecutionResult? result = _testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"+02\") }")
                    .Create());

            Assert.Equal("+03:05", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void ParsesLiteralWithMinutes()
        {
            IExecutionResult? result = _testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"+02:35\") }")
                    .Create());
            Assert.Equal("+03:40", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void ParsesLiteralWithZero()
        {
            IExecutionResult? result = _testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"+00\") }")
                    .Create());
            Assert.Equal("+01:05", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void DoesntParseLiteralWithZ()
        {
            IExecutionResult? result = _testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"Z\") }")
                    .Create());
            Assert.Null(result.ExpectQueryResult().Data);
            Assert.Equal(1, result.ExpectQueryResult().Errors!.Count);
            Assert.Null(result.ExpectQueryResult().Errors![0].Code);
            Assert.Equal(
                "Unable to deserialize string to Offset",
                result.ExpectQueryResult().Errors![0].Message);
        }

        [Fact]
        public void DoesntParseIncorrectLiteral()
        {
            IExecutionResult? result = _testExecutor
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
    }
}
