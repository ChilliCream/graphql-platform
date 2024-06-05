using System.Collections.Generic;
using System.Linq;
using HotChocolate.Execution;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime.Tests
{
    public class InstantTypeGeneralIntegrationTests
    {
        private readonly IRequestExecutor _testExecutor =
            SchemaBuilder.New()
                .AddQueryType<InstantTypeIntegrationTests.Schema.Query>()
                .AddMutationType<InstantTypeIntegrationTests.Schema.Mutation>()
                .AddNodaTime(typeof(InstantType))
                .AddType(new InstantType(InstantPattern.General))
                .Create()
                .MakeExecutable();

        [Fact]
        public void QueryReturnsUtc()
        {
            var result = _testExecutor.Execute("query { test: one }");

            Assert.Equal("2020-02-20T17:42:59Z", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void ParsesVariable()
        {
            var result = _testExecutor
                .Execute(OperationRequestBuilder.Create()
                    .SetDocument("mutation($arg: Instant!) { test(arg: $arg) }")
                    .SetVariableValues(new Dictionary<string, object?> { {"arg", "2020-02-21T17:42:59Z" }, })
                    .Build());

            Assert.Equal("2020-02-21T17:52:59Z", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void DoesntParseAnIncorrectVariable()
        {
            var result = _testExecutor
                .Execute(OperationRequestBuilder.Create()
                    .SetDocument("mutation($arg: Instant!) { test(arg: $arg) }")
                    .SetVariableValues(new Dictionary<string, object?> { {"arg", "2020-02-20T17:42:59" }, })
                    .Build());

            Assert.Null(result.ExpectQueryResult().Data);
            Assert.Equal(1, result.ExpectQueryResult().Errors!.Count);
        }

        [Fact]
        public void ParsesLiteral()
        {
            var result = _testExecutor
                .Execute(OperationRequestBuilder.Create()
                    .SetDocument("mutation { test(arg: \"2020-02-20T17:42:59Z\") }")
                    .Build());

            Assert.Equal("2020-02-20T17:52:59Z", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void DoesntParseIncorrectLiteral()
        {
            var result = _testExecutor
                .Execute(OperationRequestBuilder.Create()
                    .SetDocument("mutation { test(arg: \"2020-02-20T17:42:59\") }")
                    .Build());

            Assert.Null(result.ExpectQueryResult().Data);
            Assert.Equal(1, result.ExpectQueryResult().Errors!.Count);
            Assert.Null(result.ExpectQueryResult().Errors!.First().Code);
            Assert.Equal(
                "Unable to deserialize string to Instant",
                result.ExpectQueryResult().Errors!.First().Message);
        }
    }
}
