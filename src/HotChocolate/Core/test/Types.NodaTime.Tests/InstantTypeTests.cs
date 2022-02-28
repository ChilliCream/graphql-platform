using System;
using System.Globalization;
using System.Linq;
using System.Text;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using NodaTime.Extensions;
using NodaTime.Text;
using Xunit;

namespace HotChocolate.Types.NodaTime.Tests
{
    public class InstantTypeIntegrationTests
    {
        public static class Schema
        {
            public class Query
            {
                public Instant One => Instant.FromUtc(2020, 02, 20, 17, 42, 59).PlusNanoseconds(1234);
            }

            public class Mutation
            {
                public Instant Test(Instant arg)
                {
                    return arg + Duration.FromMinutes(10);
                }
            }
        }

        private readonly IRequestExecutor testExecutor;
        public InstantTypeIntegrationTests()
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
            IExecutionResult? result = testExecutor.Execute("query { test: one }");
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("2020-02-20T17:42:59.000001234Z", queryResult!.Data!["test"]);
        }

        [Fact]
        public void ParsesVariable()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: Instant!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "2020-02-21T17:42:59.000001234Z")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("2020-02-21T17:52:59.000001234Z", queryResult!.Data!["test"]);
        }

        [Fact]
        public void DoesntParseAnIncorrectVariable()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: Instant!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "2020-02-20T17:42:59")
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
                    .SetQuery("mutation { test(arg: \"2020-02-20T17:42:59.000001234Z\") }")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("2020-02-20T17:52:59.000001234Z", queryResult!.Data!["test"]);
        }

        [Fact]
        public void DoesntParseIncorrectLiteral()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"2020-02-20T17:42:59\") }")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Null(queryResult!.Data);
            Assert.Equal(1, queryResult!.Errors!.Count);
            Assert.Null(queryResult.Errors.First().Code);
            Assert.Equal("Unable to deserialize string to Instant", queryResult.Errors.First().Message);
        }
    }
    public class InstantTypeGeneralIntegrationTests
    {
        private readonly IRequestExecutor testExecutor;
        public InstantTypeGeneralIntegrationTests()
        {
            testExecutor = SchemaBuilder.New()
                .AddQueryType<InstantTypeIntegrationTests.Schema.Query>()
                .AddMutationType<InstantTypeIntegrationTests.Schema.Mutation>()
                .AddNodaTime(typeof(InstantType))
                .AddType(new InstantType(InstantPattern.General))
                .Create()
                .MakeExecutable();
        }

        [Fact]
        public void QueryReturnsUtc()
        {
            IExecutionResult? result = testExecutor.Execute("query { test: one }");
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("2020-02-20T17:42:59Z", queryResult!.Data!["test"]);
        }

        [Fact]
        public void ParsesVariable()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: Instant!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "2020-02-21T17:42:59Z")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("2020-02-21T17:52:59Z", queryResult!.Data!["test"]);
        }

        [Fact]
        public void DoesntParseAnIncorrectVariable()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: Instant!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "2020-02-20T17:42:59")
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
                    .SetQuery("mutation { test(arg: \"2020-02-20T17:42:59Z\") }")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("2020-02-20T17:52:59Z", queryResult!.Data!["test"]);
        }

        [Fact]
        public void DoesntParseIncorrectLiteral()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"2020-02-20T17:42:59\") }")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Null(queryResult!.Data);
            Assert.Equal(1, queryResult!.Errors!.Count);
            Assert.Null(queryResult.Errors.First().Code);
            Assert.Equal("Unable to deserialize string to Instant", queryResult.Errors.First().Message);
        }
    }
    public class InstantTypeDateTimeOffsetIntegrationTests
    {
        class InstantDateTimeOffsetPattern : IPattern<Instant>
        {
            public ParseResult<Instant> Parse(string text)
            {
                return DateTimeOffset.TryParse(text, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AssumeUniversal, out var value)
                    ? ParseResult<Instant>.ForValue(value.ToInstant())
                    : ParseResult<Instant>.ForException(() => new FormatException("Could not parse DateTimeOffset"));
            }

            public string Format(Instant value)
            {
                return InstantPattern.General.Format(value);
            }

            public StringBuilder AppendFormat(Instant value, StringBuilder builder)
            {
                return InstantPattern.General.AppendFormat(value, builder);
            }
        }

        private readonly IRequestExecutor testExecutor;
        public InstantTypeDateTimeOffsetIntegrationTests()
        {
            testExecutor = SchemaBuilder.New()
                .AddQueryType<InstantTypeIntegrationTests.Schema.Query>()
                .AddMutationType<InstantTypeIntegrationTests.Schema.Mutation>()
                .AddNodaTime(typeof(InstantType))
                .AddType(new InstantType(InstantPattern.ExtendedIso, new InstantDateTimeOffsetPattern()))
                .Create()
                .MakeExecutable();
        }

        [Fact]
        public void QueryReturnsUtc()
        {
            IExecutionResult? result = testExecutor.Execute("query { test: one }");
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("2020-02-20T17:42:59.000001234Z", queryResult!.Data!["test"]);
        }

        [Fact]
        public void ParsesVariable()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: Instant!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "2020-02-21T17:42:59.000001234Z")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("2020-02-21T17:52:59.000001234Z", queryResult!.Data!["test"]);
        }

        [Fact]
        public void DoesParseAnIncorrectExtendedVariableAsDateTimeOffset()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: Instant!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "2020-02-20T17:42:59")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("2020-02-20T17:52:59Z", queryResult!.Data!["test"]);
        }

        [Fact]
        public void ParsesLiteral()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"2020-02-20T17:42:59.000001234Z\") }")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("2020-02-20T17:52:59.000001234Z", queryResult!.Data!["test"]);
        }

        [Fact]
        public void DoesParseIncorrectExtendedLiteralAsDateTimeOffset()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"2020-02-20T17:42:59\") }")
                    .Create());
            var queryResult = result as IReadOnlyQueryResult;
            Assert.Equal("2020-02-20T17:52:59Z", queryResult!.Data!["test"]);
        }
    }
}
