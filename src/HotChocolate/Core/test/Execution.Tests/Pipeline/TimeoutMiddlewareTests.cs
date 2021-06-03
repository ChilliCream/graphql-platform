using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Options;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Tests;
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

        public class TimeoutQuery
        {
            public async Task<string> Timeout(CancellationToken cancellationToken)
            {
                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
                return "Hello";
            }
        }
    }
}
