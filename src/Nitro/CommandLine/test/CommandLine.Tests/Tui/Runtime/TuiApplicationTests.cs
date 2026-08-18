using System.Collections.Concurrent;
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
