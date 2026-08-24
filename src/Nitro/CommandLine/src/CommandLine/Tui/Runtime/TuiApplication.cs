using System.Threading.Channels;
using Spectre.Console.Rendering;

namespace ChilliCream.Nitro.CommandLine.Tui.Runtime;

/// <summary>
/// Handles one <see cref="TuiEvent"/> and reports whether the current frame is dirty
/// and needs to be repainted.
/// </summary>
internal delegate bool TuiEventHandler(TuiEvent tuiEvent);

/// <summary>
/// Produces the renderable for the current frame.
/// </summary>
internal delegate IRenderable TuiFrameRenderer();

/// <summary>
/// Runs until <paramref name="cancellationToken"/> is cancelled, writing
/// <see cref="TuiEvent"/>s into <paramref name="writer"/> as they occur.
/// Merged into the event loop alongside key input and ticks; a source that
/// cannot start (for example an unsupported file system) should return
/// without writing anything rather than throwing, so the loop degrades to
/// its other event sources instead of failing to start.
/// </summary>
internal delegate Task TuiEventSource(ChannelWriter<TuiEvent> writer, CancellationToken cancellationToken);

/// <summary>
/// Runs the TUI event loop: merges raw key input and periodic ticks into a single
/// event stream, dispatches each event to a root handler, and repaints the live
/// display only when the handler reports the frame changed.
/// </summary>
internal sealed class TuiApplication
{
    private const int EventChannelCapacity = 64;

    private static readonly TimeSpan DefaultTickInterval = TimeSpan.FromMilliseconds(100);
    private static readonly TimeSpan DefaultKeyPollInterval = TimeSpan.FromMilliseconds(15);

    /// <summary>
    /// The fixed bound the shutdown path waits for the key-reader, tick, and
    /// additional event-source tasks before giving up on them. Matches the
    /// coordinator/effect shutdown wait used elsewhere in the TUI runtime.
    /// </summary>
    private static readonly TimeSpan DefaultShutdownDrainBound = TimeSpan.FromSeconds(5);

    private readonly IAnsiConsole _console;
    private readonly TimeSpan _tickInterval;
    private readonly TimeSpan _keyPollInterval;
    private readonly TimeSpan _shutdownDrainBound;

    public TuiApplication(
        IAnsiConsole console,
        TimeSpan? tickInterval = null,
        TimeSpan? keyPollInterval = null,
        TimeSpan? shutdownDrainBound = null)
    {
        _console = console ?? throw new ArgumentNullException(nameof(console));
        _tickInterval = tickInterval ?? DefaultTickInterval;
        _keyPollInterval = keyPollInterval ?? DefaultKeyPollInterval;
        _shutdownDrainBound = shutdownDrainBound ?? DefaultShutdownDrainBound;
    }

    /// <summary>
    /// Runs the event loop until <paramref name="cancellationToken"/> is cancelled or
    /// the terminal delivers Ctrl+C, restoring the terminal before returning.
    /// </summary>
    /// <param name="rootHandler">Handles each merged event and reports whether the frame is dirty.</param>
    /// <param name="rootRenderer">Produces the renderable for the current frame.</param>
    /// <param name="cancellationToken">Stops the loop and restores the terminal when cancelled.</param>
    /// <param name="eventSources">
    /// Additional event sources (for example a data-change watcher) merged into the
    /// same channel as key input and ticks. Each runs for the lifetime of the loop
    /// and is cancelled and awaited alongside the built-in sources on every exit
    /// path, including when <paramref name="rootHandler"/> throws.
    /// </param>
    public async Task RunAsync(
        TuiEventHandler rootHandler,
        TuiFrameRenderer rootRenderer,
        CancellationToken cancellationToken,
        IReadOnlyList<TuiEventSource>? eventSources = null)
    {
        ArgumentNullException.ThrowIfNull(rootHandler);
        ArgumentNullException.ThrowIfNull(rootRenderer);

        var channel = Channel.CreateBounded<TuiEvent>(new BoundedChannelOptions(EventChannelCapacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        });

        using var loopCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        ConsoleCancelEventHandler onCancelKeyPress = (_, e) =>
        {
            // Prevent the default behavior (immediate process termination) so the
            // session below still gets to restore the terminal.
            e.Cancel = true;
            loopCts.Cancel();
        };

        Console.CancelKeyPress += onCancelKeyPress;

        try
        {
            using var session = new TerminalSession(_console);

            var keyReaderTask = Task.Run(
                () => ReadKeysAsync(channel.Writer, loopCts.Token),
                CancellationToken.None);
            var tickTask = Task.Run(
                () => ProduceTicksAsync(channel.Writer, loopCts.Token),
                CancellationToken.None);
            var additionalTasks = (eventSources ?? [])
                .Select(source => Task.Run(() => source(channel.Writer, loopCts.Token), CancellationToken.None))
                .ToArray();

            try
            {
                try
                {
                    await _console.Live(rootRenderer())
                        .StartAsync(async ctx =>
                        {
                            // Spectre's Live display only paints once something calls
                            // Refresh/UpdateTarget; without this, the initial frame stays
                            // blank until the root handler first reports a dirty event.
                            ctx.Refresh();

                            await foreach (var tuiEvent in channel.Reader.ReadAllAsync(loopCts.Token))
                            {
                                if (rootHandler(tuiEvent))
                                {
                                    ctx.UpdateTarget(rootRenderer());
                                }
                            }
                        })
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (loopCts.IsCancellationRequested)
                {
                    // Shutdown was requested (caller cancellation or Ctrl+C).
                }
            }
            finally
            {
                // Ensure the background key-reader, tick, and additional event-source
                // tasks are always stopped and awaited, even if the handler or
                // renderer throws, so stdin is never left being consumed and no
                // watcher outlives this loop after the method returns.
                loopCts.Cancel();
                channel.Writer.TryComplete();

                try
                {
                    // Bounded so a noncooperative task (one that does not observe
                    // cancellation) cannot block terminal restoration indefinitely;
                    // it is abandoned once the fixed shutdown bound elapses.
                    await Task.WhenAll([keyReaderTask, tickTask, .. additionalTasks])
                        .WaitAsync(_shutdownDrainBound, CancellationToken.None)
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // Expected once loopCts is cancelled.
                }
                catch (TimeoutException)
                {
                    // The shutdown bound elapsed with a task still running; abandon it
                    // rather than block the terminal from being restored.
                }
            }
        }
        finally
        {
            Console.CancelKeyPress -= onCancelKeyPress;
        }
    }

    private async Task ReadKeysAsync(ChannelWriter<TuiEvent> writer, CancellationToken cancellationToken)
    {
        var input = _console.Input;

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (input.IsKeyAvailable())
                {
                    var key = input.ReadKey(intercept: true);
                    if (key is { } info)
                    {
                        writer.TryWrite(new TuiEvent.KeyEvent(info));
                    }
                }
                else
                {
                    await Task.Delay(_keyPollInterval, cancellationToken).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown.
        }
    }

    private async Task ProduceTicksAsync(ChannelWriter<TuiEvent> writer, CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(_tickInterval);

        var lastWidth = _console.Profile.Width;
        var lastHeight = _console.Profile.Height;

        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                var width = _console.Profile.Width;
                var height = _console.Profile.Height;

                if (width != lastWidth || height != lastHeight)
                {
                    lastWidth = width;
                    lastHeight = height;
                    writer.TryWrite(new TuiEvent.ResizeEvent(width, height));
                }
                else
                {
                    writer.TryWrite(new TuiEvent.TickEvent(DateTimeOffset.UtcNow));
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown.
        }
    }
}
