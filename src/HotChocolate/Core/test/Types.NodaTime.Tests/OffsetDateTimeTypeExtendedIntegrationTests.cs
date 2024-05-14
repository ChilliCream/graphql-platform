using HotChocolate.Execution;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime.Tests
{
    public class OffsetDateTimeTypeExtendedIntegrationTests
    {
        private readonly IRequestExecutor testExecutor;

        public OffsetDateTimeTypeExtendedIntegrationTests()
        {
            testExecutor = SchemaBuilder.New()
                .AddQueryType<OffsetDateTimeTypeIntegrationTests.Schema.Query>()
                .AddMutationType<OffsetDateTimeTypeIntegrationTests.Schema.Mutation>()
                .AddNodaTime(typeof(OffsetDateTimeType))
                .AddType(new OffsetDateTimeType(OffsetDateTimePattern.ExtendedIso))
                .Create()
                .MakeExecutable();
        }

        [Fact]
        public void QueryReturns()
        {
            var result = testExecutor.Execute("query { test: hours }");

            Assert.Equal("2020-12-31T18:30:13.000001234+02", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void QueryReturnsWithMinutes()
        {
            var result = testExecutor.Execute("query { test: hoursAndMinutes }");

            Assert.Equal("2020-12-31T18:30:13.000001234+02:30", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void ParsesVariable()
        {
            var result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: OffsetDateTime!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "2020-12-31T18:30:13+02")
                    .Create());

            Assert.Equal("2020-12-31T18:40:13+02", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void ParsesVariableWithMinutes()
        {
            var result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: OffsetDateTime!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "2020-12-31T18:30:13+02:35")
                    .Create());

            Assert.Equal("2020-12-31T18:40:13+02:35", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void DoesntParseAnIncorrectVariable()
        {
            var result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: OffsetDateTime!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "2020-12-31T18:30:13")
                    .Create());

            Assert.Null(result.ExpectQueryResult().Data);
            Assert.Equal(1, result.ExpectQueryResult().Errors!.Count);
        }

        [Fact]
        public void ParsesLiteral()
        {
            var result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"2020-12-31T18:30:13+02\") }")
                    .Create());

            Assert.Equal("2020-12-31T18:40:13+02", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void ParsesLiteralWithMinutes()
        {
            var result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"2020-12-31T18:30:13+02:35\") }")
                    .Create());

            Assert.Equal("2020-12-31T18:40:13+02:35", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void DoesntParseIncorrectLiteral()
        {
            var result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"2020-12-31T18:30:13\") }")
                    .Create());

            Assert.Null(result.ExpectQueryResult().Data);
            Assert.Equal(1, result.ExpectQueryResult().Errors!.Count);
            Assert.Null(result.ExpectQueryResult().Errors![0].Code);
            Assert.Equal(
                "Unable to deserialize string to OffsetDateTime",
                result.ExpectQueryResult().Errors![0].Message);
        }
    }
}
