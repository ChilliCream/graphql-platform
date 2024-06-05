using System.Collections.Generic;
using HotChocolate.Execution;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime.Tests
{
    public class LocalDateTimeTypeFullRoundtripIntegrationTests
    {
        private readonly IRequestExecutor _testExecutor =
            SchemaBuilder.New()
                .AddQueryType<LocalDateTimeTypeIntegrationTests.Schema.Query>()
                .AddMutationType<LocalDateTimeTypeIntegrationTests.Schema.Mutation>()
                .AddNodaTime(typeof(LocalDateTimeType))
                .AddType(new LocalDateTimeType(LocalDateTimePattern.FullRoundtrip))
                .Create()
                .MakeExecutable();

        [Fact]
        public void QueryReturns()
        {
            var result = _testExecutor.Execute("query { test: one }");

            Assert.Equal(
                "2020-02-07T17:42:59.000001234 (Julian)",
                result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void ParsesVariable()
        {
            var result = _testExecutor
                .Execute(OperationRequestBuilder.Create()
                    .SetDocument("mutation($arg: LocalDateTime!) { test(arg: $arg) }")
                    .SetVariableValues(new Dictionary<string, object?> { {"arg", "2020-02-21T17:42:59.000001234 (Julian)" }, })
                    .Build());

            Assert.Equal(
                "2020-02-21T17:52:59.000001234 (Julian)",
                result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void DoesntParseAnIncorrectVariable()
        {
            var result = _testExecutor
                .Execute(OperationRequestBuilder.Create()
                    .SetDocument("mutation($arg: LocalDateTime!) { test(arg: $arg) }")
                    .SetVariableValues(new Dictionary<string, object?> { {"arg", "2020-02-20T17:42:59Z" }, })
                    .Build());

            Assert.Null(result.ExpectQueryResult().Data);
            Assert.Equal(1, result.ExpectQueryResult().Errors!.Count);
        }

        [Fact]
        public void ParsesLiteral()
        {
            var result = _testExecutor
                .Execute(OperationRequestBuilder.Create()
                    .SetDocument("mutation { test(arg: \"2020-02-20T17:42:59.000001234 (Julian)\") }")
                    .Build());

            Assert.Equal(
                "2020-02-20T17:52:59.000001234 (Julian)",
                result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void DoesntParseIncorrectLiteral()
        {
            var result = _testExecutor
                .Execute(OperationRequestBuilder.Create()
                    .SetDocument("mutation { test(arg: \"2020-02-20T17:42:59Z\") }")
                    .Build());

            Assert.Null(result.ExpectQueryResult().Data);
            Assert.Equal(1, result.ExpectQueryResult().Errors!.Count);
            Assert.Null(result.ExpectQueryResult().Errors![0].Code);
            Assert.Equal(
                "Unable to deserialize string to LocalDateTime",
                result.ExpectQueryResult().Errors![0].Message);
        }
    }
}
