using HotChocolate.Execution.Processing;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution;

public class ResolverCleanupTests
{
    [Fact]
    public async Task RegisterForCleanup_Should_RunRequestCleanupWhenTheResultIsDisposed_When_CleanAfterIsRequest()
    {
        // arrange
        List<string> log = [];
        var executor = await CreateExecutorAsync(
            context =>
            {
                context.RegisterForCleanup(() => Log(log, "resolver"), CleanAfter.Resolver);
                context.RegisterForCleanup(() => Log(log, "request"), CleanAfter.Request);
            });

        // act
        var result = await executor.ExecuteAsync("{ foo }", TestContext.Current.CancellationToken);
        var logBeforeDispose = log.ToArray();
        await result.DisposeAsync();
        await result.DisposeAsync();

        // assert
        Assert.Equal(["resolver"], logBeforeDispose);
        Assert.Equal(["resolver", "request"], log);
    }

    [Fact]
    public async Task RegisterForCleanup_Should_RunRequestCleanupOnce_When_TheRequestIsCancelled()
    {
        // arrange
        List<string> log = [];
        using var cancellation = new CancellationTokenSource();
        var executor = await CreateExecutorAsync(
            context =>
            {
                context.RegisterForCleanup(() => Log(log, "request"), CleanAfter.Request);
                cancellation.Cancel();
            });

        // act
        var result = await executor.ExecuteAsync("{ foo }", cancellation.Token);
        await result.DisposeAsync();

        // assert
        Assert.Equal(["request"], log);
    }

    [Fact]
    public async Task RegisterForCleanup_Should_RunDeferredRequestCleanup_When_CancelledBeforeTheInitialPayload()
    {
        // arrange
        List<string> log = [];
        var registered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cancellation = new CancellationTokenSource();
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(
                descriptor =>
                {
                    descriptor
                        .Field("block")
                        .Type<StringType>()
                        .Resolve(async _ =>
                        {
                            await registered.Task.ConfigureAwait(false);
                            cancellation.Cancel();
                            await Task.Delay(Timeout.InfiniteTimeSpan, cancellation.Token).ConfigureAwait(false);
                            return string.Empty;
                        });
                    descriptor
                        .Field("deferred")
                        .Type<StringType>()
                        .Use(next => context =>
                        {
                            context.RegisterForCleanup(() => Log(log, "request"), CleanAfter.Request);
                            registered.TrySetResult();
                            return next(context);
                        })
                        .Resolve("deferred");
                })
            .ModifyOptions(o => o.EnableDefer = true)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            "{ block ... @defer { deferred } }",
            cancellation.Token);
        await result.DisposeAsync();

        // assert
        Assert.Equal(["request"], log);
    }

    [Fact]
    public async Task RegisterForCleanup_Should_RunRootRequestCleanupOnce_When_CancelledBeforeTheInitialPayload()
    {
        // arrange
        var cleanupCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var resolverStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseResolver = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cleanupCount = 0;
        using var cancellation = new CancellationTokenSource();
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(
                descriptor =>
                {
                    descriptor
                        .Field("block")
                        .Type<StringType>()
                        .Use(next => async context =>
                        {
                            resolverStarted.TrySetResult();
                            await releaseResolver.Task.ConfigureAwait(false);
                            context.RegisterForCleanup(
                                () =>
                                {
                                    Interlocked.Increment(ref cleanupCount);
                                    cleanupCompleted.TrySetResult();
                                    return default;
                                },
                                CleanAfter.Request);
                            await next(context).ConfigureAwait(false);
                        })
                        .Resolve("block");
                    descriptor
                        .Field("deferred")
                        .Type<StringType>()
                        .Resolve("deferred");
                })
            .ModifyOptions(o => o.EnableDefer = true)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var execution = executor.ExecuteAsync(
            "{ block ... @defer { deferred } }",
            cancellation.Token);
        await resolverStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
        cancellation.Cancel();
        var result = await execution;
        releaseResolver.TrySetResult();
        await cleanupCompleted.Task.WaitAsync(TestContext.Current.CancellationToken);
        await result.DisposeAsync();
        await result.DisposeAsync();

        // assert
        Assert.Equal(1, cleanupCount);
    }

    [Fact]
    public async Task RegisterForCleanup_Should_KeepBufferedResultOwnership_When_DeferIsDisabled()
    {
        // arrange
        List<string> log = [];
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(
                descriptor => descriptor
                    .Field("foo")
                    .Type<StringType>()
                    .Use(next => context =>
                    {
                        context.RegisterForCleanup(() => Log(log, "request"), CleanAfter.Request);
                        return next(context);
                    })
                    .Resolve("bar"))
            .ModifyOptions(o => o.EnableDefer = true)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);
        var request = OperationRequestBuilder.New()
            .SetDocument("query($defer: Boolean!) { foo ... @defer(if: $defer) { foo } }")
            .SetVariableValues("""{ "defer": false }""")
            .Build();

        // act
        var result = await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);
        var logBeforeDispose = log.ToArray();
        await result.DisposeAsync();
        await result.DisposeAsync();

        // assert
        Assert.Empty(logBeforeDispose);
        Assert.Equal(["request"], log);
    }

    [Fact]
    public async Task RegisterForCleanup_Should_RunRequestCleanupForEachContext_When_AVariableBatchIsCancelled()
    {
        // arrange
        List<string> log = [];
        var registrations = 0;
        using var cancellation = new CancellationTokenSource();
        var executor = await CreateExecutorAsync(
            context =>
            {
                context.RegisterForCleanup(() => Log(log, "request"), CleanAfter.Request);

                if (Interlocked.Increment(ref registrations) == 2)
                {
                    cancellation.Cancel();
                }
            });
        var request = OperationRequestBuilder.New()
            .SetDocument("query($n: Int) { foo(n: $n) }")
            .SetVariableValues("""[{ "n": 1 }, { "n": 2 }]""")
            .Build();

        // act
        var result = await executor.ExecuteAsync(request, cancellation.Token);
        await result.DisposeAsync();

        // assert
        Assert.Equal(["request", "request"], log);
    }

    [Fact]
    public async Task RequestCleanup_Should_RunRemainingActions_When_AnActionThrows()
    {
        // arrange
        var cleanup = new RequestCleanup();
        var cleanupException = new InvalidOperationException("cleanup failed");
        var completed = 0;
        cleanup.Register(() => ValueTask.FromException(cleanupException));
        cleanup.Register(
            () =>
            {
                completed++;
                return default;
            });

        // act
        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() => cleanup.ExecuteAsync().AsTask());
        await cleanup.ExecuteAsync();

        // assert
        Assert.Same(cleanupException, thrown);
        Assert.Equal(1, completed);
    }

    [Fact]
    public async Task RequestCleanup_Should_RunLateRegistrationOnce_When_TheCollectorWasDrained()
    {
        // arrange
        var cleanup = new RequestCleanup();
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var registrations = 0;
        await cleanup.ExecuteAsync();

        // act
        cleanup.Register(
            () =>
            {
                Interlocked.Increment(ref registrations);
                completed.TrySetResult();
                return default;
            });
        await completed.Task.WaitAsync(TestContext.Current.CancellationToken);
        await cleanup.ExecuteAsync();

        // assert
        Assert.Equal(1, registrations);
    }

    [Fact]
    public async Task OperationResultBuilder_Should_RunRemainingActions_When_AnActionThrows()
    {
        // arrange
        var builder = new OperationResultBuilder();
        var cleanupException = new InvalidOperationException("cleanup failed");
        var completed = 0;
        builder.RegisterForCleanup(() => ValueTask.FromException(cleanupException));
        builder.RegisterForCleanup(
            () =>
            {
                completed++;
                return default;
            });

        // act
        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(
            () => builder.ExecuteCleanupTasksAsync().AsTask());
        await builder.ExecuteCleanupTasksAsync();

        // assert
        Assert.Same(cleanupException, thrown);
        Assert.Equal(1, completed);
    }

    private static async Task<IRequestExecutor> CreateExecutorAsync(Action<IMiddlewareContext> register)
        => await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(
                descriptor => descriptor
                    .Field("foo")
                    .Argument("n", a => a.Type<IntType>())
                    .Type<StringType>()
                    .Use(next => context =>
                    {
                        register(context);
                        return next(context);
                    })
                    .Resolve("bar"))
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

    private static ValueTask Log(List<string> log, string entry)
    {
        log.Add(entry);
        return default;
    }
}
