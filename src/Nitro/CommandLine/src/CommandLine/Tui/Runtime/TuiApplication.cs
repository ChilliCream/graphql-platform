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
/// Runs the TUI event loop: merges raw key input and periodic ticks into a single
/// event stream, dispatches each event to a root handler, and repaints the live
/// display only when the handler reports the frame changed.
/// </summary>
internal sealed class TuiApplication
{
    private const int EventChannelCapacity = 64;

    private static readonly TimeSpan DefaultTickInterval = TimeSpan.FromMilliseconds(100);
    private static readonly TimeSpan DefaultKeyPollInterval = TimeSpan.FromMilliseconds(15);

    private readonly IAnsiConsole _console;
    private readonly TimeSpan _tickInterval;
    private readonly TimeSpan _keyPollInterval;

    public TuiApplication(
        IAnsiConsole console,
        TimeSpan? tickInterval = null,
        TimeSpan? keyPollInterval = null)
    {
        _console = console ?? throw new ArgumentNullException(nameof(console));
        _tickInterval = tickInterval ?? DefaultTickInterval;
        _keyPollInterval = keyPollInterval ?? DefaultKeyPollInterval;
    }

    /// <summary>
    /// Runs the event loop until <paramref name="cancellationToken"/> is cancelled or
    /// the terminal delivers Ctrl+C, restoring the terminal before returning.
    /// </summary>
    public async Task RunAsync(
        TuiEventHandler rootHandler,
        TuiFrameRenderer rootRenderer,
        CancellationToken cancellationToken)
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

            try
            {
                await _console.Live(rootRenderer())
                    .StartAsync(async ctx =>
                    {
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

            channel.Writer.TryComplete();
            await Task.WhenAll(keyReaderTask, tickTask).ConfigureAwait(false);
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
