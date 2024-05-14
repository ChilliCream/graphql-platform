using HotChocolate.Execution;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime.Tests
{
    public class OffsetTimeTypeExtendedIntegrationTests
    {
        private readonly IRequestExecutor testExecutor;

        public OffsetTimeTypeExtendedIntegrationTests()
        {
            testExecutor = SchemaBuilder.New()
                .AddQueryType<OffsetTimeTypeIntegrationTests.Schema.Query>()
                .AddMutationType<OffsetTimeTypeIntegrationTests.Schema.Mutation>()
                .AddNodaTime(typeof(OffsetTimeType))
                .AddType(new OffsetTimeType(OffsetTimePattern.ExtendedIso))
                .Create()
                .MakeExecutable();
        }

        [Fact]
        public void QueryReturns()
        {
            var result = testExecutor.Execute("query { test: hours }");

            Assert.Equal("18:30:13.010011234+02", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void QueryReturnsWithMinutes()
        {
            var result = testExecutor.Execute("query { test: hoursAndMinutes }");

            Assert.Equal("18:30:13.010011234+02:35", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void ParsesVariable()
        {
            var result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: OffsetTime!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "18:30:13.010011234+02")
                    .Create());

            Assert.Equal("18:30:13.010011234+02", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void ParsesVariableWithMinutes()
        {
            var result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: OffsetTime!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "18:30:13.010011234+02:35")
                    .Create());

            Assert.Equal("18:30:13.010011234+02:35", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void DoesntParseAnIncorrectVariable()
        {
            var result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: OffsetTime!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "18:30:13.010011234")
                    .Create());

            Assert.Null(result.ExpectQueryResult().Data);
            Assert.Equal(1, result.ExpectQueryResult().Errors!.Count);
        }

        [Fact]
        public void ParsesLiteral()
        {
            var result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"18:30:13.010011234+02\") }")
                    .Create());

            Assert.Equal("18:30:13.010011234+02", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void ParsesLiteralWithMinutes()
        {
            var result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"18:30:13.010011234+02:35\") }")
                    .Create());

            Assert.Equal("18:30:13.010011234+02:35", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void DoesntParseIncorrectLiteral()
        {
            var result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"18:30:13.010011234\") }")
                    .Create());

            Assert.Null(result.ExpectQueryResult().Data);
            Assert.Equal(1, result.ExpectQueryResult().Errors!.Count);
            Assert.Null(result.ExpectQueryResult().Errors![0].Code);
            Assert.Equal(
                "Unable to deserialize string to OffsetTime",
                result.ExpectQueryResult().Errors![0].Message);
        }
    }
}
