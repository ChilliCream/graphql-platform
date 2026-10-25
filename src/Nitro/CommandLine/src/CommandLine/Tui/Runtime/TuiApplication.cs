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
/// cannot start should return without writing anything rather than throwing.
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

    private static readonly TimeSpan s_defaultTickInterval = TimeSpan.FromMilliseconds(100);
    private static readonly TimeSpan s_defaultKeyPollInterval = TimeSpan.FromMilliseconds(15);

    /// <summary>
    /// The default shutdown wait for background event-source tasks.
    /// </summary>
    private static readonly TimeSpan s_defaultShutdownDrainBound = TimeSpan.FromSeconds(5);

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
        _tickInterval = tickInterval ?? s_defaultTickInterval;
        _keyPollInterval = keyPollInterval ?? s_defaultKeyPollInterval;
        _shutdownDrainBound = shutdownDrainBound ?? s_defaultShutdownDrainBound;
    }

    /// <summary>
    /// Runs the event loop until <paramref name="cancellationToken"/> is cancelled or
    /// the terminal delivers Ctrl+C, restoring the terminal before returning.
    /// </summary>
    /// <param name="rootHandler">Handles each merged event and reports whether the frame is dirty.</param>
    /// <param name="rootRenderer">Produces the renderable for the current frame.</param>
    /// <param name="cancellationToken">Stops the loop and restores the terminal when cancelled.</param>
    /// <param name="eventSources">
    /// Additional event sources sharing the input channel; null adds none.
    /// On exit, their cancellation is requested and they are awaited with the built-in
    /// sources up to the configured shutdown bound.
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
            // Prevent the default behavior (immediate process termination).
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
                            // Paint the initial frame.
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
                // Request cancellation of every event source on exit.
                loopCts.Cancel();
                channel.Writer.TryComplete();

                try
                {
                    // Wait for event sources up to the configured shutdown bound.
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
                    // The shutdown bound elapsed with a task still running; abandon it.
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
