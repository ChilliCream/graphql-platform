using System.Collections.Generic;
using HotChocolate.Execution;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime.Tests
{
    public class OffsetDateTypeFullRoundtripIntegrationTests
    {
        private readonly IRequestExecutor _testExecutor =
            SchemaBuilder.New()
                .AddQueryType<OffsetDateTypeIntegrationTests.Schema.Query>()
                .AddMutationType<OffsetDateTypeIntegrationTests.Schema.Mutation>()
                .AddNodaTime(typeof(OffsetDateType))
                .AddType(new OffsetDateType(OffsetDatePattern.FullRoundtrip))
                .Create()
                .MakeExecutable();

        [Fact]
        public void QueryReturns()
        {
            var result = _testExecutor.Execute("query { test: hours }");

            Assert.Equal("2020-12-31+02 (Gregorian)", result.ExpectQueryResult()!.Data!["test"]);
        }

        [Fact]
        public void QueryReturnsWithMinutes()
        {
            var result = _testExecutor.Execute("query { test: hoursAndMinutes }");

            Assert.Equal("2020-12-31+02:35 (Gregorian)", result.ExpectQueryResult()!.Data!["test"]);
        }

        [Fact]
        public void ParsesVariable()
        {
            var result = _testExecutor
                .Execute(OperationRequestBuilder.Create()
                    .SetDocument("mutation($arg: OffsetDate!) { test(arg: $arg) }")
                    .SetVariableValues(new Dictionary<string, object?> { {"arg", "2020-12-31+02 (Gregorian)" }, })
                    .Build());

            Assert.Equal("2020-12-31+02 (Gregorian)", result.ExpectQueryResult()!.Data!["test"]);
        }

        [Fact]
        public void ParsesVariableWithMinutes()
        {
            var result = _testExecutor
                .Execute(OperationRequestBuilder.Create()
                    .SetDocument("mutation($arg: OffsetDate!) { test(arg: $arg) }")
                    .SetVariableValues(new Dictionary<string, object?> { {"arg", "2020-12-31+02:35 (Gregorian)" }, })
                    .Build());

            Assert.Equal("2020-12-31+02:35 (Gregorian)", result.ExpectQueryResult()!.Data!["test"]);
        }

        [Fact]
        public void DoesntParseAnIncorrectVariable()
        {
            var result = _testExecutor
                .Execute(OperationRequestBuilder.Create()
                    .SetDocument("mutation($arg: OffsetDate!) { test(arg: $arg) }")
                    .SetVariableValues(new Dictionary<string, object?> { {"arg", "2020-12-31 (Gregorian)" }, })
                    .Build());

            Assert.Null(result.ExpectQueryResult()!.Data);
            Assert.Equal(1, result.ExpectQueryResult()!.Errors!.Count);
        }

        [Fact]
        public void ParsesLiteral()
        {
            var result = _testExecutor
                .Execute(OperationRequestBuilder.Create()
                    .SetDocument("mutation { test(arg: \"2020-12-31+02 (Gregorian)\") }")
                    .Build());

            Assert.Equal("2020-12-31+02 (Gregorian)", result.ExpectQueryResult()!.Data!["test"]);
        }

        [Fact]
        public void ParsesLiteralWithMinutes()
        {
            var result = _testExecutor
                .Execute(OperationRequestBuilder.Create()
                    .SetDocument("mutation { test(arg: \"2020-12-31+02:35 (Gregorian)\") }")
                    .Build());

            Assert.Equal("2020-12-31+02:35 (Gregorian)", result.ExpectQueryResult()!.Data!["test"]);
        }

        [Fact]
        public void DoesntParseIncorrectLiteral()
        {
            var result = _testExecutor
                .Execute(OperationRequestBuilder.Create()
                    .SetDocument("mutation { test(arg: \"2020-12-31 (Gregorian)\") }")
                    .Build());

            Assert.Null(result.ExpectQueryResult()!.Data);
            Assert.Equal(1, result.ExpectQueryResult()!.Errors!.Count);
            Assert.Null(result.ExpectQueryResult().Errors![0].Code);
            Assert.Equal(
                "Unable to deserialize string to OffsetDate",
                result.ExpectQueryResult().Errors![0].Message);
        }
    }
}
