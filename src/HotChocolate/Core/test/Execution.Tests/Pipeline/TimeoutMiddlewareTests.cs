using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Options;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Tests;
using HotChocolate.Types;
using HotChocolate.Utilities;
using HotChocolate.Validation;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Execution.Pipeline
{
    public class TimeoutMiddlewareTests
    {
        [Fact]
        public async Task TimeoutMiddleware_Is_Integrated_Into_DefaultPipeline()
        {
            Snapshot.FullName();

            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<TimeoutQuery>()
                .ModifyRequestOptions(o => o.ExecutionTimeout = TimeSpan.FromMilliseconds(100))
                .ExecuteRequestAsync("{ timeout }")
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task Ensure_That_Combined_Token_Is_Not_Disposed_On_Stream()
        {
            using var cts = new CancellationTokenSource();

            IExecutionResult result = await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<TimeoutQuery>()
                .AddSubscriptionType<Subscriptions>()
                .ExecuteRequestAsync("subscription { onFoo }", cancellationToken: cts.Token);

            SubscriptionResult stream = Assert.IsType<SubscriptionResult>(result);

            IAsyncEnumerable<IQueryResult> enumerable = stream.ReadResultsAsync();
            IAsyncEnumerator<IQueryResult> enumerator = enumerable.GetAsyncEnumerator(
                CancellationToken.None);

            // we should get the first result!
            await enumerator.MoveNextAsync();
            enumerator.Current.ToJson().MatchSnapshot();

            cts.Cancel();

            Assert.False(await enumerator.MoveNextAsync(), "the stream should be canceled.");
        }

        public class TimeoutQuery
        {
            public async Task<string> Timeout(CancellationToken cancellationToken)
            {
                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
                return "Hello";
            }
        }

        public class Subscriptions
        {
            [SubscribeAndResolve]
            public async IAsyncEnumerable<string> OnFoo(
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    yield return await Task.FromResult("a");
                }

                if (!cancellationToken.IsCancellationRequested)
                {
                    yield return "b";
                    throw new Exception("we should not get here in this test case.");
                }
            }
        }
    }
}
