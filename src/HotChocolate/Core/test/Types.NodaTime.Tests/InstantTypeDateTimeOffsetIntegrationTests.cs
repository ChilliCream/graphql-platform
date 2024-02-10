using System;
using System.Globalization;
using System.Text;
using HotChocolate.Execution;
using NodaTime;
using NodaTime.Extensions;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime.Tests
{
    public class InstantTypeDateTimeOffsetIntegrationTests
    {
        class InstantDateTimeOffsetPattern : IPattern<Instant>
        {
            public ParseResult<Instant> Parse(string text)
            {
                return DateTimeOffset.TryParse(
                        text,
                        DateTimeFormatInfo.InvariantInfo,
                        DateTimeStyles.AssumeUniversal,
                        out var value)
                    ? ParseResult<Instant>.ForValue(value.ToInstant())
                    : ParseResult<Instant>
                        .ForException(() => new FormatException("Could not parse DateTimeOffset"));
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
                .AddType(
                    new InstantType(InstantPattern.ExtendedIso, new InstantDateTimeOffsetPattern()))
                .Create()
                .MakeExecutable();
        }

        [Fact]
        public void QueryReturnsUtc()
        {
            IExecutionResult? result = testExecutor.Execute("query { test: one }");

            Assert.Equal(
                "2020-02-20T17:42:59.000001234Z",
                result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void ParsesVariable()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: Instant!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "2020-02-21T17:42:59.000001234Z")
                    .Create());

            Assert.Equal(
                "2020-02-21T17:52:59.000001234Z",
                result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void DoesParseAnIncorrectExtendedVariableAsDateTimeOffset()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation($arg: Instant!) { test(arg: $arg) }")
                    .SetVariableValue("arg", "2020-02-20T17:42:59")
                    .Create());

            Assert.Equal("2020-02-20T17:52:59Z", result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void ParsesLiteral()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"2020-02-20T17:42:59.000001234Z\") }")
                    .Create());

            Assert.Equal(
                "2020-02-20T17:52:59.000001234Z",
                result.ExpectQueryResult().Data!["test"]);
        }

        [Fact]
        public void DoesParseIncorrectExtendedLiteralAsDateTimeOffset()
        {
            IExecutionResult? result = testExecutor
                .Execute(QueryRequestBuilder.New()
                    .SetQuery("mutation { test(arg: \"2020-02-20T17:42:59\") }")
                    .Create());

            Assert.Equal("2020-02-20T17:52:59Z", result.ExpectQueryResult().Data!["test"]);
        }
    }
}
