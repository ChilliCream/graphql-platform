using ChilliCream.Nitro.CommandLine.Tui.Input;

namespace ChilliCream.Nitro.CommandLine.Tui.Shell;

/// <summary>
/// A mode that can capture raw key input before semantic message dispatch.
/// </summary>
internal interface IRawKeyCapturingMode
{
    /// <summary>
    /// Whether the mode currently captures raw key input.
    /// </summary>
    bool IsInputCapturing { get; }

    /// <summary>
    /// The hints for the current input capture, read only while
    /// <see cref="IsInputCapturing"/> is true.
    /// </summary>
    IReadOnlyList<KeyHint> CapturingHints { get; }

    /// <summary>
    /// Handles one raw key while <see cref="IsInputCapturing"/> is true,
    /// returning zero or more follow-up messages for the shell to dispatch
    /// in turn.
    /// </summary>
    IReadOnlyList<TuiMessage> HandleRawKey(ConsoleKeyInfo info);
}
