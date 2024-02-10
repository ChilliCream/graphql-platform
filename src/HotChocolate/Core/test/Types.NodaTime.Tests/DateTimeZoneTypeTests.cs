using System.Linq;
using HotChocolate.Execution;
using NodaTime;

namespace HotChocolate.Types.NodaTime.Tests
{
    public class DateTimeZoneTypeIntegrationTests
    {
        public static class Schema
        {
            public class Query
            {
                public DateTimeZone Utc => DateTimeZone.Utc;
                public DateTimeZone Rome => DateTimeZoneProviders.Tzdb["Europe/Rome"];
                public DateTimeZone Chihuahua => DateTimeZoneProviders.Tzdb["America/Chihuahua"];
            }

            public class Mutation
            {
                public string Test(DateTimeZone arg)
                {
                    return arg.Id;
                }
            }
        }

        private readonly IRequestExecutor testExecutor;

        public DateTimeZoneTypeIntegrationTests()
        {
            testExecutor = SchemaBuilder.New()
                .AddQueryType<Schema.Query>()
                .AddMutationType<Schema.Mutation>()
                .AddNodaTime()
                .Create()
                .MakeExecutable();
        }

        [Fact]
        public void QueryReturnsUtc()
        {
            IExecutionResult result =  testExecutor.Execute("query { test: utc }");
            Assert.Equal("UTC", Assert.IsType<QueryResult>(result).Data!["test"]);
        }

        [Fact]
        public void QueryReturnsRome()
        {
            IExecutionResult result = testExecutor.Execute("query { test: rome }");
            Assert.Equal("Europe/Rome", Assert.IsType<QueryResult>(result).Data!["test"]);
        }

        [Fact]
        public void QueryReturnsChihuahua()
        {
            IExecutionResult result = testExecutor.Execute("query { test: chihuahua }");
            Assert.Equal("America/Chihuahua", Assert.IsType<QueryResult>(result).Data!["test"]);
        }

        [Fact]
        public void ParsesVariable()
        {
            IExecutionResult result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: DateTimeZone!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "Europe/Amsterdam")
                    .Create());
            Assert.Equal("Europe/Amsterdam", Assert.IsType<QueryResult>(result).Data!["test"]);
        }

        [Fact]
        public void DoesntParseIncorrectVariable()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: DateTimeZone!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "Europe/Hamster")
                    .Create());
            Assert.Null(Assert.IsType<QueryResult>(result).Data);
            Assert.Equal(1, Assert.IsType<QueryResult>(result).Errors!.Count);
        }

        [Fact]
        public void ParsesLiteral()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"Europe/Amsterdam\") }")
                    .Create());
            Assert.Equal("Europe/Amsterdam", Assert.IsType<QueryResult>(result).Data!["test"]);
        }

        [Fact]
        public void DoesntParseIncorrectLiteral()
        {
            IExecutionResult result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"Europe/Hamster\") }")
                    .Create());
            Assert.Null(Assert.IsType<QueryResult>(result).Data);
            Assert.Equal(1, Assert.IsType<QueryResult>(result).Errors!.Count);
            Assert.Null(Assert.IsType<QueryResult>(result).Errors!.First().Code);
            Assert.Equal(
                "Unable to deserialize string to DateTimeZone",
                Assert.IsType<QueryResult>(result).Errors!.First().Message);
        }
    }
}
