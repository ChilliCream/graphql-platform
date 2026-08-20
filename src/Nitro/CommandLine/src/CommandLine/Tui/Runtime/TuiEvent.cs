namespace ChilliCream.Nitro.CommandLine.Tui.Runtime;

/// <summary>
/// A single input to the TUI event loop.
/// </summary>
internal abstract record TuiEvent
{
    private TuiEvent()
    {
    }

    /// <summary>
    /// A key was read from the terminal.
    /// </summary>
    public sealed record KeyEvent(ConsoleKeyInfo Info) : TuiEvent;

    /// <summary>
    /// The terminal window size changed.
    /// </summary>
    public sealed record ResizeEvent(int Width, int Height) : TuiEvent;

    /// <summary>
    /// A periodic tick fired with no other event pending.
    /// </summary>
    public sealed record TickEvent(DateTimeOffset Now) : TuiEvent;

    /// <summary>
    /// Data outside the terminal changed, so the current frame may be stale.
    /// </summary>
    public sealed record DataChangedEvent : TuiEvent;
}
