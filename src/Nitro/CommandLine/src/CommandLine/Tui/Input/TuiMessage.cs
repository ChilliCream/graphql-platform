namespace ChilliCream.Nitro.CommandLine.Tui.Input;

/// <summary>
/// A semantic intent produced by the keymap layer. Modes and the shell handle these
/// instead of raw key input.
/// </summary>
internal abstract record TuiMessage
{
    private TuiMessage()
    {
    }

    /// <summary>
    /// The user asked to quit. The shell is expected to confirm before exiting.
    /// </summary>
    public sealed record QuitRequested : TuiMessage;

    /// <summary>
    /// The pending quit was confirmed.
    /// </summary>
    public sealed record ConfirmQuit : TuiMessage;

    /// <summary>
    /// The pending quit was cancelled.
    /// </summary>
    public sealed record CancelQuit : TuiMessage;

    /// <summary>
    /// The active mode should reload its data.
    /// </summary>
    public sealed record RefreshRequested : TuiMessage;

    /// <summary>
    /// The selection cursor should move one step in <paramref name="Direction"/>.
    /// </summary>
    public sealed record MoveCursor(CursorDirection Direction) : TuiMessage;

    /// <summary>
    /// The selection cursor should jump to <paramref name="Edge"/> of the current list.
    /// </summary>
    public sealed record MoveToEdge(EdgeTarget Edge) : TuiMessage;

    /// <summary>
    /// The active view should change by <paramref name="Delta"/> positions.
    /// </summary>
    public sealed record CycleView(int Delta) : TuiMessage;

    /// <summary>
    /// The active mode should toggle its maximized layout.
    /// </summary>
    public sealed record ToggleMaximize : TuiMessage;

    /// <summary>
    /// The current selection should be opened.
    /// </summary>
    public sealed record OpenSelected : TuiMessage;

    /// <summary>
    /// The identifier of the current selection should be copied.
    /// </summary>
    public sealed record CopySelectedId : TuiMessage;

    /// <summary>
    /// A toast with <paramref name="Text"/> should be shown using <paramref name="Style"/>.
    /// </summary>
    public sealed record ShowToast(string Text, ToastStyle Style) : TuiMessage;
}

/// <summary>
/// A direction the selection cursor can move in.
/// </summary>
internal enum CursorDirection
{
    Up,
    Down,
    Left,
    Right
}

/// <summary>
/// An edge of a list the selection cursor can jump to.
/// </summary>
internal enum EdgeTarget
{
    Top,
    Bottom
}

/// <summary>
/// The visual style of a toast message.
/// </summary>
internal enum ToastStyle
{
    Info,
    Success,
    Warn,
    Error
}
