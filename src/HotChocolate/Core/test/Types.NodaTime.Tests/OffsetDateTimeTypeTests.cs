using System;
using System.Linq;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using NodaTime.Text;
using Xunit;

namespace HotChocolate.Types.NodaTime.Tests
{
    public class OffsetDateTimeTypeIntegrationTests
    {
        public static class Schema
        {
            public class Query
            {
                public OffsetDateTime Hours =>
                    OffsetDateTime.FromDateTimeOffset(
                        new DateTimeOffset(2020, 12, 31, 18, 30, 13, TimeSpan.FromHours(2))).PlusNanoseconds(1234);

                public OffsetDateTime HoursAndMinutes =>
                    OffsetDateTime.FromDateTimeOffset(
                        new DateTimeOffset(2020, 12, 31, 18, 30, 13, TimeSpan.FromHours(2) + TimeSpan.FromMinutes(30))).PlusNanoseconds(1234);
            }

            public class Mutation
            {
                public OffsetDateTime Test(OffsetDateTime arg)
                {
                    return arg + Duration.FromMinutes(10);
                }
            }
        }

        private readonly IRequestExecutor testExecutor;
        public OffsetDateTimeTypeIntegrationTests()
        {
            testExecutor = SchemaBuilder.New()
                .AddQueryType<Schema.Query>()
                .AddMutationType<Schema.Mutation>()
                .AddNodaTime()
                .Create()
                .MakeExecutable();
        }

        [Fact]
        public void QueryReturns()
        {
            IExecutionResult? result = testExecutor.Execute("query { test: hours }");
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("2020-12-31T18:30:13+02", queryResult!.Data!["test"]);
        }

        [Fact]
        public void QueryReturnsWithMinutes()
        {
            IExecutionResult? result = testExecutor.Execute("query { test: hoursAndMinutes }");
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("2020-12-31T18:30:13+02:30", queryResult!.Data!["test"]);
        }

        [Fact]
        public void ParsesVariable()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: OffsetDateTime!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "2020-12-31T18:30:13+02")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("2020-12-31T18:40:13+02", queryResult!.Data!["test"]);
        }

        [Fact]
        public void ParsesVariableWithMinutes()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: OffsetDateTime!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "2020-12-31T18:30:13+02:35")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("2020-12-31T18:40:13+02:35", queryResult!.Data!["test"]);
        }

        [Fact]
        public void DoesntParseAnIncorrectVariable()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: OffsetDateTime!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "2020-12-31T18:30:13")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Null(queryResult!.Data);
            Assert.Equal(1, queryResult!.Errors!.Count);
        }

        [Fact]
        public void ParsesLiteral()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"2020-12-31T18:30:13+02\") }")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("2020-12-31T18:40:13+02", queryResult!.Data!["test"]);
        }

        [Fact]
        public void ParsesLiteralWithMinutes()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"2020-12-31T18:30:13+02:35\") }")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("2020-12-31T18:40:13+02:35", queryResult!.Data!["test"]);
        }

        [Fact]
        public void DoesntParseIncorrectLiteral()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"2020-12-31T18:30:13\") }")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Null(queryResult!.Data);
            Assert.Equal(1, queryResult!.Errors!.Count);
            Assert.Null(queryResult.Errors.First().Code);
            Assert.Equal("Unable to deserialize string to OffsetDateTime", queryResult.Errors.First().Message);
        }
    }

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
            IExecutionResult? result = testExecutor.Execute("query { test: hours }");
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("2020-12-31T18:30:13.000001234+02", queryResult!.Data!["test"]);
        }

        [Fact]
        public void QueryReturnsWithMinutes()
        {
            IExecutionResult? result = testExecutor.Execute("query { test: hoursAndMinutes }");
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("2020-12-31T18:30:13.000001234+02:30", queryResult!.Data!["test"]);
        }

        [Fact]
        public void ParsesVariable()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: OffsetDateTime!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "2020-12-31T18:30:13+02")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("2020-12-31T18:40:13+02", queryResult!.Data!["test"]);
        }

        [Fact]
        public void ParsesVariableWithMinutes()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: OffsetDateTime!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "2020-12-31T18:30:13+02:35")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("2020-12-31T18:40:13+02:35", queryResult!.Data!["test"]);
        }

        [Fact]
        public void DoesntParseAnIncorrectVariable()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: OffsetDateTime!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "2020-12-31T18:30:13")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Null(queryResult!.Data);
            Assert.Equal(1, queryResult!.Errors!.Count);
        }

        [Fact]
        public void ParsesLiteral()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"2020-12-31T18:30:13+02\") }")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("2020-12-31T18:40:13+02", queryResult!.Data!["test"]);
        }

        [Fact]
        public void ParsesLiteralWithMinutes()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"2020-12-31T18:30:13+02:35\") }")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("2020-12-31T18:40:13+02:35", queryResult!.Data!["test"]);
        }

        [Fact]
        public void DoesntParseIncorrectLiteral()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"2020-12-31T18:30:13\") }")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Null(queryResult!.Data);
            Assert.Equal(1, queryResult!.Errors!.Count);
            Assert.Null(queryResult.Errors.First().Code);
            Assert.Equal("Unable to deserialize string to OffsetDateTime", queryResult.Errors.First().Message);
        }
    }
    public class OffsetDateTimeTypeGeneralIntegrationTests
    {
        private readonly IRequestExecutor testExecutor;
        public OffsetDateTimeTypeGeneralIntegrationTests()
        {
            testExecutor = SchemaBuilder.New()
                .AddQueryType<OffsetDateTimeTypeIntegrationTests.Schema.Query>()
                .AddMutationType<OffsetDateTimeTypeIntegrationTests.Schema.Mutation>()
                .AddNodaTime(typeof(OffsetDateTimeType))
                .AddType(new OffsetDateTimeType(OffsetDateTimePattern.GeneralIso))
                .Create()
                .MakeExecutable();
        }

        [Fact]
        public void QueryReturns()
        {
            IExecutionResult? result = testExecutor.Execute("query { test: hours }");
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("2020-12-31T18:30:13+02", queryResult!.Data!["test"]);
        }

        [Fact]
        public void QueryReturnsWithMinutes()
        {
            IExecutionResult? result = testExecutor.Execute("query { test: hoursAndMinutes }");
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("2020-12-31T18:30:13+02:30", queryResult!.Data!["test"]);
        }

        [Fact]
        public void ParsesVariable()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: OffsetDateTime!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "2020-12-31T18:30:13+02")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("2020-12-31T18:40:13+02", queryResult!.Data!["test"]);
        }

        [Fact]
        public void ParsesVariableWithMinutes()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: OffsetDateTime!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "2020-12-31T18:30:13+02:35")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("2020-12-31T18:40:13+02:35", queryResult!.Data!["test"]);
        }

        [Fact]
        public void DoesntParseAnIncorrectVariable()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: OffsetDateTime!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "2020-12-31T18:30:13")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Null(queryResult!.Data);
            Assert.Equal(1, queryResult!.Errors!.Count);
        }

        [Fact]
        public void ParsesLiteral()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"2020-12-31T18:30:13+02\") }")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("2020-12-31T18:40:13+02", queryResult!.Data!["test"]);
        }

        [Fact]
        public void ParsesLiteralWithMinutes()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"2020-12-31T18:30:13+02:35\") }")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("2020-12-31T18:40:13+02:35", queryResult!.Data!["test"]);
        }

        [Fact]
        public void DoesntParseIncorrectLiteral()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"2020-12-31T18:30:13\") }")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Null(queryResult!.Data);
            Assert.Equal(1, queryResult!.Errors!.Count);
            Assert.Null(queryResult.Errors.First().Code);
            Assert.Equal("Unable to deserialize string to OffsetDateTime", queryResult.Errors.First().Message);
        }
    }
    public class OffsetDateTimeTypeRfc3339IntegrationTests
    {
        private readonly IRequestExecutor testExecutor;
        public OffsetDateTimeTypeRfc3339IntegrationTests()
        {
            testExecutor = SchemaBuilder.New()
                .AddQueryType<OffsetDateTimeTypeIntegrationTests.Schema.Query>()
                .AddMutationType<OffsetDateTimeTypeIntegrationTests.Schema.Mutation>()
                .AddNodaTime(typeof(OffsetDateTimeType))
                .AddType(new OffsetDateTimeType(OffsetDateTimePattern.Rfc3339))
                .Create()
                .MakeExecutable();
        }

        [Fact]
        public void QueryReturns()
        {
            IExecutionResult? result = testExecutor.Execute("query { test: hours }");
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("2020-12-31T18:30:13.000001234+02:00", queryResult!.Data!["test"]);
        }

        [Fact]
        public void QueryReturnsWithMinutes()
        {
            IExecutionResult? result = testExecutor.Execute("query { test: hoursAndMinutes }");
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("2020-12-31T18:30:13.000001234+02:30", queryResult!.Data!["test"]);
        }

        [Fact]
        public void ParsesVariable()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: OffsetDateTime!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "2020-12-31T18:30:13.000001234+02:00")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("2020-12-31T18:40:13.000001234+02:00", queryResult!.Data!["test"]);
        }

        [Fact]
        public void ParsesVariableWithMinutes()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: OffsetDateTime!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "2020-12-31T18:30:13+02:35")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("2020-12-31T18:40:13+02:35", queryResult!.Data!["test"]);
        }

        [Fact]
        public void DoesntParseAnIncorrectVariable()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: OffsetDateTime!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "2020-12-31T18:30:13")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Null(queryResult!.Data);
            Assert.Equal(1, queryResult!.Errors!.Count);
        }

        [Fact]
        public void ParsesLiteral()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"2020-12-31T18:30:13.000001234+02:00\") }")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("2020-12-31T18:40:13.000001234+02:00", queryResult!.Data!["test"]);
        }

        [Fact]
        public void ParsesLiteralWithMinutes()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"2020-12-31T18:30:13+02:35\") }")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("2020-12-31T18:40:13+02:35", queryResult!.Data!["test"]);
        }

        [Fact]
        public void DoesntParseIncorrectLiteral()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"2020-12-31T18:30:13\") }")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Null(queryResult!.Data);
            Assert.Equal(1, queryResult!.Errors!.Count);
            Assert.Null(queryResult.Errors.First().Code);
            Assert.Equal("Unable to deserialize string to OffsetDateTime", queryResult.Errors.First().Message);
        }
    }
}
