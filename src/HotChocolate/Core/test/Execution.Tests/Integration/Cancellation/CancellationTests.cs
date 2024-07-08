using System.Diagnostics;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate;

public class CancellationTests
{
    [Fact]
    public async Task Serial_Ensure_Execution_Waits_For_Tasks()
    {
        // arrange
        var query = new Query1();

        var executor =
            await new ServiceCollection()
                .AddSingleton(query)
                .AddGraphQL()
                .AddQueryType<Query1>()
                .BuildRequestExecutorAsync();

        using var cts = new CancellationTokenSource(150);

        // act
        await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ task1 task2 }")
                .Build(),
                cts.Token);

        // assert
        // the first task is completed
        Assert.True(query.Task1);
        Assert.True(query.Task1Done);

        // the second never started
        Assert.False(query.Task2);
        Assert.False(query.Task2Done);
    }

    [Fact]
    public async Task Parallel_Ensure_Execution_Waits_For_Tasks()
        => await TryTest(async ct =>
            {
                // arrange
                var query = new Query2();

                var executor =
                    await new ServiceCollection()
                        .AddSingleton(query)
                        .AddGraphQL()
                        .AddQueryType<Query2>()
                        .BuildRequestExecutorAsync(cancellationToken: ct);

                using var cts = new CancellationTokenSource(150);

                // act
                await executor.ExecuteAsync("{ task1 task2 }", cts.Token);

                // assert
                // the first task is completed
                Assert.True(query.Task1);
                Assert.True(query.Task1Done);

                // the second task is completed
                Assert.True(query.Task2);
                Assert.True(query.Task2Done);
            });

    public class Query1
    {
        [GraphQLIgnore]
        public bool Task1 { get; set; }

        [GraphQLIgnore]
        public bool Task1Done { get; set; }

        [GraphQLIgnore]
        public bool Task2 { get; set; }

        [GraphQLIgnore]
        public bool Task2Done { get; set; }

        [Serial]
        public async Task<string> GetTask1()
        {
            Task1 = true;
            await Task.Delay(400);
            Task1Done = true;
            return "foo";
        }

        [Serial]
        public async Task<string> GetTask2()
        {
            Task2 = true;
            await Task.Delay(200);
            Task2Done = true;
            return "bar";
        }
    }

    public class Query2
    {
        [GraphQLIgnore]
        public bool Task1 { get; set; }

        [GraphQLIgnore]
        public bool Task1Done { get; set; }

        [GraphQLIgnore]
        public bool Task2 { get; set; }

        [GraphQLIgnore]
        public bool Task2Done { get; set; }

        public async Task<string> GetTask1()
        {
            Task1 = true;
            await Task.Delay(400);
            Task1Done = true;
            return "foo";
        }

        public async Task<string> GetTask2()
        {
            Task2 = true;
            await Task.Delay(200);
            Task2Done = true;
            return "bar";
        }
    }

    protected static async Task TryTest(Func<CancellationToken, Task> action)
    {
        // we will try four times ....
        using var cts = new CancellationTokenSource(Debugger.IsAttached ? 600_000_000 : 60_000);
        var ct = cts.Token;
        var count = 0;
        var wait = 50;

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            if (count < 3)
            {
                try
                {
                    await action(ct).ConfigureAwait(false);
                    break;
                }
                catch
                {
                    // try again
                }
            }
            else
            {
                await action(ct).ConfigureAwait(false);
                break;
            }

            await Task.Delay(wait, ct).ConfigureAwait(false);
            wait *= 2;
            count++;
        }
    }
}
