using ChilliCream.Nitro.CommandLine.Tui.Input;

namespace ChilliCream.Nitro.CommandLine.Tui.Shell;

/// <summary>
/// An <see cref="ITuiMode"/> that owns modal overlays needing raw key input
/// (text fields, for example) rather than the semantic <see cref="TuiMessage"/>
/// dispatch every other gesture goes through. <see cref="TuiShell"/> checks
/// <see cref="IsInputCapturing"/> on the active tab's mode instead of a
/// concrete type check, so any mode can opt into owning its own overlays.
/// </summary>
internal interface IRawKeyCapturingMode
{
    /// <summary>
    /// Whether this mode currently owns an active overlay that must consume
    /// raw key input directly.
    /// </summary>
    bool IsInputCapturing { get; }

    /// <summary>
    /// The footer hints for whichever overlay is currently capturing input.
    /// Read only while <see cref="IsInputCapturing"/> is true.
    /// </summary>
    IReadOnlyList<KeyHint> CapturingHints { get; }

    /// <summary>
    /// Handles one raw key while <see cref="IsInputCapturing"/> is true,
    /// returning zero or more follow-up messages for the shell to dispatch
    /// in turn.
    /// </summary>
    IReadOnlyList<TuiMessage> HandleRawKey(ConsoleKeyInfo info);
}
