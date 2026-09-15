using System.Text.Json;
using CookieCrumble;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Composite;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution;

public class BatchResolverDeferTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeferredBranch_Should_AwaitCleanup_When_BatchCleanupSuspends(bool cleanupThrows)
    {
        // arrange
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var observed = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        var executor = await new ServiceCollection().AddGraphQL()
            .AddQueryType(d => d.Field("value")
                .Argument("id", a => a.Type<NonNullType<IntType>>())
                .Type<IntType>()
                .ResolveBatch(contexts =>
                {
                    var context = (IMiddlewareContext)contexts[0];
                    var variables = context.Variables;
                    var services = context.RequestServices;
                    context.RegisterForCleanup(async () =>
                    {
                        entered.SetResult();
                        await release.Task;
                        try
                        {
                            observed.SetResult(new
                            {
                                VariablesAlive = ReferenceEquals(variables, context.Variables),
                                ServicesAlive = ReferenceEquals(services, context.RequestServices),
                                Argument = context.ArgumentValue<int>("id")
                            });
                        }
                        catch (Exception ex)
                        {
                            observed.SetException(ex);
                        }

                        if (cleanupThrows)
                        {
                            throw new InvalidOperationException("Cleanup failed.");
                        }
                    });
                    return new ValueTask<IReadOnlyList<ResolverResult>>([ResolverResult.Ok(1)]);
                }))
            .ModifyOptions(o => o.EnableDefer = true)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        await using var result = await executor.ExecuteAsync(OperationRequestBuilder.New()
            .SetDocument("query($id:Int!){ ... @defer { value(id:$id) } }")
            .SetVariableValues(new Dictionary<string, object?> { ["id"] = 1 })
            .Build(), cancellationToken: TestContext.Current.CancellationToken);
        var stream = Assert.IsType<ResponseStream>(result);
        await using var enumerator = stream.ReadResultsAsync()
            .GetAsyncEnumerator(TestContext.Current.CancellationToken);
        var payloads = new List<string>();
        await enumerator.MoveNextAsync();
        await using (var initial = enumerator.Current)
        {
            payloads.Add(initial.ToJson());
        }

        await entered.Task;
        var next = enumerator.MoveNextAsync().AsTask();
        var completedBeforeCleanup = next.IsCompleted;
        release.SetResult();
        var contextState = await observed.Task;
        while (await next)
        {
            await using (var response = enumerator.Current)
            {
                payloads.Add(response.ToJson());
            }

            next = enumerator.MoveNextAsync().AsTask();
        }

        // assert
        Assert.False(completedBeforeCleanup);
        new Snapshot(postFix: cleanupThrows.ToString())
            .Add(contextState, "Context after cleanup suspension")
            .Add(payloads, "Payloads")
            .MatchMarkdownSnapshot();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Defer_Should_Report_Error_In_Owning_Payload_When_Batch_Entry_Fails(bool reportError)
    {
        // arrange
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(d =>
            {
                d.Field("immediate").Resolve("ready");
                d.Field("product")
                    .Type<NonNullType<StringType>>()
                    .ResolveBatch(async contexts =>
                    {
                        await release.Task.WaitAsync(TestContext.Current.CancellationToken);

                        if (reportError)
                        {
                            contexts[0].ReportError("Deferred product failed.");
                        }

                        return new[] { ResolverResult.Ok(null) };
                    });
            })
            .ModifyOptions(o => o.EnableDefer = true)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        await using var result = await executor.ExecuteAsync(
            "{ immediate ... @defer(label: \"product\") { product } }",
            cancellationToken: TestContext.Current.CancellationToken);
        var payloads = await DrainPayloadsAsync(result, release)
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        // assert
        var snapshot = new Snapshot(postFix: reportError.ToString());

        foreach (var payload in payloads)
        {
            snapshot.Add(payload, "Payload", "json");
        }

        snapshot.MatchMarkdownSnapshot();
    }

    private static async Task<List<string>> DrainPayloadsAsync(
        IExecutionResult result,
        TaskCompletionSource release)
    {
        var payloads = new List<string>();
        var stream = Assert.IsType<ResponseStream>(result);

        await foreach (var response in stream.ReadResultsAsync())
        {
            await using (response)
            {
                payloads.Add(response.ToJson());
                release.TrySetResult();
            }
        }

        return payloads;
    }
}
