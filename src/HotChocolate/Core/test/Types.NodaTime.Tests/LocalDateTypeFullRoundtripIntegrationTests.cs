using System.Collections.Generic;
using HotChocolate.Execution;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime.Tests
{
    public class LocalDateTypeFullRoundtripIntegrationTests
    {
        private readonly IRequestExecutor _testExecutor =
            SchemaBuilder.New()
                .AddQueryType<LocalDateTypeIntegrationTests.Schema.Query>()
                .AddMutationType<LocalDateTypeIntegrationTests.Schema.Mutation>()
                .AddNodaTime(typeof(LocalDateType))
                .AddType(new LocalDateType(LocalDatePattern.FullRoundtrip))
                .Create()
                .MakeExecutable();

        [Fact]
        public void QueryReturns()
        {
            var result = _testExecutor.Execute("query { test: one }");

            Assert.Equal("5780-05-25 (Hebrew Civil)", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void ParsesVariable()
        {
            IExecutionResult? result = _testExecutor
                .Execute(
                    OperationRequestBuilder.Create()
                        .SetDocument("mutation($arg: LocalDate!) { test(arg: $arg) }")
                        .SetVariableValues(new Dictionary<string, object?> { { "arg", "2020-02-21 (Hebrew Civil)" }, })
                        .Build());

            Assert.Equal("2020-02-24 (Hebrew Civil)", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void DoesntParseAnIncorrectVariable()
        {
            IExecutionResult? result = _testExecutor
                .Execute(
                    OperationRequestBuilder.Create()
                        .SetDocument("mutation($arg: LocalDate!) { test(arg: $arg) }")
                        .SetVariableValues(
                            new Dictionary<string, object?> { { "arg", "2020-02-20T17:42:59 (Hebrew Civil)" }, })
                        .Build());

            Assert.Null(result.ExpectQueryResult().Data);
            Assert.Equal(1, result.ExpectQueryResult().Errors!.Count);
        }

        [Fact]
        public void ParsesLiteral()
        {
            var result = _testExecutor
                .Execute(
                    OperationRequestBuilder.Create()
                        .SetDocument("mutation { test(arg: \"2020-02-20 (Hebrew Civil)\") }")
                        .Build());

            Assert.Equal("2020-02-23 (Hebrew Civil)", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void DoesntParseIncorrectLiteral()
        {
            var result = _testExecutor
                .Execute(
                    OperationRequestBuilder.Create()
                        .SetDocument("mutation { test(arg: \"2020-02-20T17:42:59 (Hebrew Civil)\") }")
                        .Build());

            Assert.Null(result.ExpectQueryResult().Data);
            Assert.Equal(1, result.ExpectQueryResult().Errors!.Count);
            Assert.Null(result.ExpectQueryResult().Errors![0].Code);
            Assert.Equal(
                "Unable to deserialize string to LocalDate",
                result.ExpectQueryResult().Errors![0].Message);
        }
    }
}
