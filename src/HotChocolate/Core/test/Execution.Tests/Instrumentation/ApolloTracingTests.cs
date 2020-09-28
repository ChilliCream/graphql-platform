using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Options;
using Snapshooter.Xunit;
using Xunit;
using static HotChocolate.Tests.TestHelper;

namespace HotChocolate
{
    public class DiagnosticsEventsTests
    {
        [Fact]
        public async Task ApolloTracing_Always()
        {
            // arrange
            IRequestExecutor executor = await CreateExecutorAsync(c => c
                .AddDocumentFromString(
                    @"
                    type Query {
                        a: String
                    }")
                .AddResolver("Query", "a", () => "hello world a")
                .AddApolloTracing(TracingPreference.Always, new TestTimestampProvider()));

            // act
            IExecutionResult result = await executor.ExecuteAsync("{ a }");

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task ApolloTracing_Always_Parsing_And_Validation_Is_Cached()
        {
            // arrange
            IRequestExecutor executor = await CreateExecutorAsync(c => c
                .AddDocumentFromString(
                    @"
                    type Query {
                        a: String
                    }")
                .AddResolver("Query", "a", () => "hello world a")
                .AddApolloTracing(TracingPreference.Always, new TestTimestampProvider()));

            // act
            IExecutionResult result = await executor.ExecuteAsync("{ a }");

            // the second execution will not parse or validate since these steps are cached.
            result = await executor.ExecuteAsync("{ a }");

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task ApolloTracing_OnDemand_NoHeader()
        {
            // arrange
            IRequestExecutor executor = await CreateExecutorAsync(c => c
                .AddDocumentFromString(
                    @"
                    type Query {
                        a: String
                    }")
                .AddResolver("Query", "a", () => "hello world a")
                .AddApolloTracing(TracingPreference.OnDemand, new TestTimestampProvider()));

            // act
            IExecutionResult result = await executor.ExecuteAsync("{ a }");

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task ApolloTracing_OnDemand_WithHeader()
        {
            // arrange
            IRequestExecutor executor = await CreateExecutorAsync(c => c
                .AddDocumentFromString(
                    @"
                    type Query {
                        a: String
                    }")
                .AddResolver("Query", "a", () => "hello world a")
                .AddApolloTracing(TracingPreference.OnDemand, new TestTimestampProvider()));

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ a }")
                    .SetProperty(ContextDataKeys.EnableTracing, true)
                    .Create());

            // assert
            result.ToJson().MatchSnapshot();
        }

        private class TestTimestampProvider : ITimestampProvider
        {
            private DateTime _utcNow = new DateTime(2010, 10, 10, 12, 00, 00);
            private long _nowInNanoseconds = 10;

            public DateTime UtcNow()
            {
                DateTime time = _utcNow;
                _utcNow = _utcNow.AddMilliseconds(50);
                return time;
            }

            public long NowInNanoseconds()
            {
                return _nowInNanoseconds += 20;
            }
        }
    }
}
