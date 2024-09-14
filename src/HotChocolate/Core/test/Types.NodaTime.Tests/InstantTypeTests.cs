using HotChocolate.Execution;
using NodaTime;

namespace HotChocolate.Types.NodaTime.Tests
{
    public class InstantTypeIntegrationTests
    {
        public static class Schema
        {
            public class Query
            {
                public Instant One
                    => Instant.FromUtc(2020, 02, 20, 17, 42, 59).PlusNanoseconds(1234);
            }

            public class Mutation
            {
                public Instant Test(Instant arg)
                {
                    return arg + Duration.FromMinutes(10);
                }
            }
        }

        private readonly IRequestExecutor _testExecutor = SchemaBuilder.New()
            .AddQueryType<Schema.Query>()
            .AddMutationType<Schema.Mutation>()
            .AddNodaTime()
            .Create()
            .MakeExecutable();

        [Fact]
        public void QueryReturnsUtc()
        {
            var result = _testExecutor.Execute("query { test: one }");

            Assert.Equal(
                "2020-02-20T17:42:59.000001234Z",
                result.ExpectOperationResult().Data!["test"]);
        }

        [Fact]
        public void ParsesVariable()
        {
            var result = _testExecutor
                .Execute(OperationRequestBuilder.New()
                    .SetDocument("mutation($arg: Instant!) { test(arg: $arg) }")
                    .SetVariableValues(new Dictionary<string, object?> { {"arg", "2020-02-21T17:42:59.000001234Z" }, })
                    .Build());

            Assert.Equal(
                "2020-02-21T17:52:59.000001234Z",
                result.ExpectOperationResult().Data!["test"]);
        }

        [Fact]
        public void DoesntParseAnIncorrectVariable()
        {
            var result = _testExecutor
                .Execute(OperationRequestBuilder.New()
                    .SetDocument("mutation($arg: Instant!) { test(arg: $arg) }")
                    .SetVariableValues(new Dictionary<string, object?> { {"arg", "2020-02-20T17:42:59" }, })
                    .Build());

            Assert.Null(result.ExpectOperationResult().Data);
            Assert.Single(result.ExpectOperationResult().Errors!);
        }

        [Fact]
        public void ParsesLiteral()
        {
            var result = _testExecutor
                .Execute(OperationRequestBuilder.New()
                    .SetDocument("mutation { test(arg: \"2020-02-20T17:42:59.000001234Z\") }")
                    .Build());

            Assert.Equal(
                "2020-02-20T17:52:59.000001234Z",
                result.ExpectOperationResult().Data!["test"]);
        }

        [Fact]
        public void DoesntParseIncorrectLiteral()
        {
            var result = _testExecutor
                .Execute(OperationRequestBuilder.New()
                    .SetDocument("mutation { test(arg: \"2020-02-20T17:42:59\") }")
                    .Build());

            Assert.Null(result.ExpectOperationResult().Data);
            Assert.Single(result.ExpectOperationResult().Errors!);
            Assert.Null(result.ExpectOperationResult().Errors![0].Code);
            Assert.Equal(
                "Unable to deserialize string to Instant",
                result.ExpectOperationResult().Errors![0].Message);
        }

        [Fact]
        public void PatternEmpty_ThrowSchemaException()
        {
            static object Call() => new InstantType([]);
            Assert.Throws<SchemaException>(Call);
        }
    }
}
