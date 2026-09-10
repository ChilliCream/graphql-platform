using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Channels;
using ChilliCream.Nitro.CommandLine.Tui.Runtime;
using Spectre.Console;
using Spectre.Console.Rendering;
using Spectre.Console.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Runtime;

public sealed class TuiApplicationTests
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromMilliseconds(10);
    private static readonly TimeSpan KeyPollInterval = TimeSpan.FromMilliseconds(5);
    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(5);

    [Fact]
    public async Task RunAsync_Should_MergeKeyAndTickEvents_IntoRootHandler()
    {
        // arrange
        var testToken = TestContext.Current.CancellationToken;
        var console = new TestConsole();
        console.Input.PushKey(ConsoleKey.A);
        var app = new TuiApplication(console, TickInterval, KeyPollInterval);
        var received = new ConcurrentQueue<TuiEvent>();
        var sawBoth = new TaskCompletionSource();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(testToken);

        bool Handler(TuiEvent tuiEvent)
        {
            received.Enqueue(tuiEvent);
            if (received.OfType<TuiEvent.KeyEvent>().Any() && received.OfType<TuiEvent.TickEvent>().Any())
            {
                sawBoth.TrySetResult();
            }

            return false;
        }

        // act
        var runTask = app.RunAsync(Handler, () => new Text("frame"), cts.Token);
        await Task.WhenAny(sawBoth.Task, Task.Delay(TestTimeout, testToken));
        cts.Cancel();
        await runTask;

        // assert
        Assert.Contains(received, e => e is TuiEvent.KeyEvent { Info.Key: ConsoleKey.A });
        Assert.Contains(received, e => e is TuiEvent.TickEvent);
    }

    [Fact]
    public async Task RunAsync_Should_EmitResizeEvent_When_ConsoleWindowSizeChangesOnTick()
    {
        // arrange
        var testToken = TestContext.Current.CancellationToken;
        var console = new TestConsole();
        var app = new TuiApplication(console, TickInterval, KeyPollInterval);
        var received = new ConcurrentQueue<TuiEvent>();
        var sawResize = new TaskCompletionSource();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(testToken);

        bool Handler(TuiEvent tuiEvent)
        {
            received.Enqueue(tuiEvent);
            if (tuiEvent is TuiEvent.ResizeEvent)
            {
                sawResize.TrySetResult();
            }

            return false;
        }

        // act
        var runTask = app.RunAsync(Handler, () => new Text("frame"), cts.Token);
        await Task.Delay(TickInterval * 3, testToken);
        console.Profile.Width = 120;
        console.Profile.Height = 40;
        await Task.WhenAny(sawResize.Task, Task.Delay(TestTimeout, testToken));
        cts.Cancel();
        await runTask;

        // assert
        var resize = Assert.Single(received.OfType<TuiEvent.ResizeEvent>());
        Assert.Equal(120, resize.Width);
        Assert.Equal(40, resize.Height);
    }

    [Fact]
    public async Task RunAsync_Should_NotRepaint_When_HandlerReportsFrameNotDirty()
    {
        // arrange
        var testToken = TestContext.Current.CancellationToken;
        var console = new TestConsole();
        var app = new TuiApplication(console, TickInterval, KeyPollInterval);
        var rendererCallCount = 0;
        var handlerCallCount = 0;
        var handlerInvoked = new TaskCompletionSource();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(testToken);

        IRenderable Renderer()
        {
            Interlocked.Increment(ref rendererCallCount);
            return new Text("frame");
        }

        bool Handler(TuiEvent tuiEvent)
        {
            Interlocked.Increment(ref handlerCallCount);
            handlerInvoked.TrySetResult();
            return false;
        }

        // act
        var runTask = app.RunAsync(Handler, Renderer, cts.Token);
        await Task.WhenAny(handlerInvoked.Task, Task.Delay(TestTimeout, testToken));
        await Task.Delay(TickInterval * 3, testToken);
        cts.Cancel();
        await runTask;

        // assert
        Assert.True(handlerCallCount > 0);
        Assert.Equal(1, rendererCallCount);
    }

    [Fact]
    public async Task RunAsync_Should_RestoreTerminal_When_CancellationRequested()
    {
        // arrange
        var testToken = TestContext.Current.CancellationToken;
        var console = new TestConsole { EmitAnsiSequences = true };
        var app = new TuiApplication(console, TickInterval, KeyPollInterval);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(testToken);

        // act
        var runTask = app.RunAsync(_ => false, () => new Text("frame"), cts.Token);
        await Task.Delay(TickInterval * 3, testToken);
        cts.Cancel();
        var completed = await Task.WhenAny(runTask, Task.Delay(TestTimeout, testToken));

        // assert
        Assert.Same(runTask, completed);
        await runTask;
        Assert.Contains("[?1049h", console.Output);
        Assert.Contains("[?1049l", console.Output);
    }

    [Fact]
    public async Task RunAsync_Should_PaintInitialFrame_BeforeAnyDirtyEvent()
    {
        // arrange
        var testToken = TestContext.Current.CancellationToken;
        var console = new TestConsole { EmitAnsiSequences = true };
        var app = new TuiApplication(console, TickInterval, KeyPollInterval);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(testToken);

        // act
        // The handler never reports the frame as dirty, so the initial frame can only
        // reach the console output via the Live display's own startup paint.
        var runTask = app.RunAsync(_ => false, () => new Text("initial-frame-marker"), cts.Token);
        await Task.Delay(TickInterval * 5, testToken);
        cts.Cancel();
        await runTask;

        // assert
        Assert.Contains("initial-frame-marker", console.Output);
    }

    [Fact]
    public async Task RunAsync_Should_MergeAdditionalEventSource_IntoRootHandler()
    {
        // arrange
        var testToken = TestContext.Current.CancellationToken;
        var console = new TestConsole();
        var app = new TuiApplication(console, TickInterval, KeyPollInterval);
        var received = new ConcurrentQueue<TuiEvent>();
        var sawDataChanged = new TaskCompletionSource();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(testToken);

        bool Handler(TuiEvent tuiEvent)
        {
            received.Enqueue(tuiEvent);
            if (tuiEvent is TuiEvent.DataChangedEvent)
            {
                sawDataChanged.TrySetResult();
            }

            return false;
        }

        async Task Source(ChannelWriter<TuiEvent> writer, CancellationToken sourceToken)
        {
            try
            {
                await Task.Delay(TickInterval, sourceToken);
                writer.TryWrite(new TuiEvent.DataChangedEvent());
                await Task.Delay(Timeout.InfiniteTimeSpan, sourceToken);
            }
            catch (OperationCanceledException)
            {
                // Expected on shutdown.
            }
        }

        // act
        var runTask = app.RunAsync(Handler, () => new Text("frame"), cts.Token, [Source]);
        await Task.WhenAny(sawDataChanged.Task, Task.Delay(TestTimeout, testToken));
        cts.Cancel();
        var completed = await Task.WhenAny(runTask, Task.Delay(TestTimeout, testToken));

        // assert
        Assert.Contains(received, e => e is TuiEvent.DataChangedEvent);
        Assert.Same(runTask, completed);
        await runTask;
    }

    [Fact]
    public async Task RunAsync_Should_KeepDeliveringTickEvents_While_SlowEffectRuns()
    {
        // A slow effect submitted from within the handler must not block the loop
        // from continuing to deliver key/render events.
        // arrange
        var testToken = TestContext.Current.CancellationToken;
        var console = new TestConsole();
        var app = new TuiApplication(console, TickInterval, KeyPollInterval);
        var queue = new TuiEffectQueue<string>();
        var release = new TaskCompletionSource();
        var submitted = false;
        var tickCountAfterSubmit = 0;
        var manyTicksObserved = new TaskCompletionSource();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(testToken);

        bool Handler(TuiEvent tuiEvent)
        {
            if (!submitted)
            {
                submitted = true;
                queue.TrySubmit(
                    "slow-op",
                    async (_, ct) =>
                    {
                        await release.Task.WaitAsync(TestTimeout, ct);
                        return "done";
                    },
                    testToken,
                    out _);
            }
            else if (tuiEvent is TuiEvent.TickEvent)
            {
                if (Interlocked.Increment(ref tickCountAfterSubmit) >= 5)
                {
                    manyTicksObserved.TrySetResult();
                }
            }

            return false;
        }

        // act
        var runTask = app.RunAsync(Handler, () => new Text("frame"), cts.Token, [queue.RunAsync]);
        var completed = await Task.WhenAny(manyTicksObserved.Task, Task.Delay(TestTimeout, testToken));

        // assert
        Assert.Same(manyTicksObserved.Task, completed);
        Assert.Equal(1, queue.PendingCount);

        release.SetResult();
        cts.Cancel();
        await runTask;
    }

    [Fact]
    public async Task RunAsync_Should_DeliverEffectCompletedEvent_When_MergedEffectQueueCompletes()
    {
        // arrange
        var testToken = TestContext.Current.CancellationToken;
        var console = new TestConsole();
        var app = new TuiApplication(console, TickInterval, KeyPollInterval);
        var queue = new TuiEffectQueue<string>();
        var received = new ConcurrentQueue<TuiEvent>();
        var sawEffectCompleted = new TaskCompletionSource();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(testToken);

        bool Handler(TuiEvent tuiEvent)
        {
            received.Enqueue(tuiEvent);
            if (tuiEvent is TuiEvent.EffectCompletedEvent)
            {
                sawEffectCompleted.TrySetResult();
            }

            return false;
        }

        // act
        var runTask = app.RunAsync(Handler, () => new Text("frame"), cts.Token, [queue.RunAsync]);
        queue.TrySubmit("op", (_, _) => Task.FromResult("stored"), testToken, out var operationId);
        await Task.WhenAny(sawEffectCompleted.Task, Task.Delay(TestTimeout, testToken));
        cts.Cancel();
        await runTask;

        // assert
        Assert.Contains(received, e => e is TuiEvent.EffectCompletedEvent);
        var completed = Assert.IsType<TuiEffectCompletion<string>.Completed>(Assert.Single(queue.DrainCompletions()));
        Assert.Equal(operationId, completed.OperationId);
        Assert.Equal("stored", completed.Result);
    }

    [Fact]
    public async Task RunAsync_Should_RestoreTerminal_WithinShutdownBound_When_EventSourceIsNoncooperative()
    {
        // A noncooperative event source (one that never observes cancellation) must
        // not block terminal restoration past the fixed shutdown bound.
        // arrange
        var testToken = TestContext.Current.CancellationToken;
        var console = new TestConsole { EmitAnsiSequences = true };
        var shutdownDrainBound = TimeSpan.FromMilliseconds(100);
        var app = new TuiApplication(console, TickInterval, KeyPollInterval, shutdownDrainBound);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(testToken);

        Task NoncooperativeSource(ChannelWriter<TuiEvent> writer, CancellationToken sourceToken)
        {
            // Deliberately ignores sourceToken so it never completes on its own.
            return Task.Delay(Timeout.InfiniteTimeSpan, CancellationToken.None);
        }

        // act
        var runTask = app.RunAsync(_ => false, () => new Text("frame"), cts.Token, [NoncooperativeSource]);
        await Task.Delay(TickInterval * 3, testToken);
        var stopwatch = Stopwatch.StartNew();
        cts.Cancel();
        var completed = await Task.WhenAny(runTask, Task.Delay(TestTimeout, testToken));

        // assert
        Assert.Same(runTask, completed);
        await runTask;
        Assert.True(
            stopwatch.Elapsed < shutdownDrainBound + TestTimeout,
            $"Shutdown took {stopwatch.Elapsed}, expected close to the {shutdownDrainBound} bound.");
        Assert.Contains("[?1049h", console.Output);
        Assert.Contains("[?1049l", console.Output);
    }

    [Fact]
    public async Task RunAsync_Should_RestoreTerminal_When_RendererThrows()
    {
        // arrange
        var testToken = TestContext.Current.CancellationToken;
        var console = new TestConsole { EmitAnsiSequences = true };
        var app = new TuiApplication(console, TickInterval, KeyPollInterval);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(testToken);

        IRenderable ThrowingRenderer() => throw new InvalidOperationException("render boom");

        // act
        var runTask = app.RunAsync(_ => true, ThrowingRenderer, cts.Token);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => runTask);

        // assert
        Assert.Equal("render boom", exception.Message);
        Assert.Contains("[?1049h", console.Output);
        Assert.Contains("[?1049l", console.Output);
    }

    [Fact]
    public async Task RunAsync_Should_StopKeyReader_When_HandlerThrows()
    {
        // arrange
        var testToken = TestContext.Current.CancellationToken;
        var console = new TestConsole();
        var app = new TuiApplication(console, TickInterval, KeyPollInterval);
        var handlerInvoked = new TaskCompletionSource();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(testToken);

        bool Handler(TuiEvent tuiEvent)
        {
            handlerInvoked.TrySetResult();
            throw new InvalidOperationException("boom");
        }

        // act
        var runTask = app.RunAsync(Handler, () => new Text("frame"), cts.Token);
        await Task.WhenAny(handlerInvoked.Task, Task.Delay(TestTimeout, testToken));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => runTask);

        // Give the key-reader loop several poll intervals worth of time to consume
        // the key below if it were still running.
        await Task.Delay(KeyPollInterval * 10, testToken);
        console.Input.PushKey(ConsoleKey.A);
        await Task.Delay(KeyPollInterval * 10, testToken);

        // assert
        Assert.Equal("boom", exception.Message);
        Assert.True(console.Input.IsKeyAvailable());
    }
}
