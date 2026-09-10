using ChilliCream.Nitro.CommandLine.Tui.Input;
using Spectre.Console.Rendering;

namespace ChilliCream.Nitro.CommandLine.Tui.Shell;

/// <summary>
/// A full-screen mode hosted by <see cref="TuiShell"/>. Mode instances live for the
/// application lifetime so their state survives the shell switching away and back.
/// </summary>
internal interface ITuiMode
{
    /// <summary>
    /// The mode-specific key table, checked before the global key table.
    /// </summary>
    KeyMap? KeyMap { get; }

    /// <summary>
    /// Called when the mode becomes the active mode.
    /// </summary>
    void OnEnter();

    /// <summary>
    /// Called when the available content area changes size.
    /// </summary>
    void OnResize(int width, int height);

    /// <summary>
    /// Handles <paramref name="message"/>, returning zero or more follow-up messages
    /// for the shell to dispatch in turn.
    /// </summary>
    IReadOnlyList<TuiMessage> Handle(TuiMessage message);

    /// <summary>
    /// Renders the mode into the given content area.
    /// </summary>
    IRenderable Render(int width, int height);

    /// <summary>
    /// The id of the mode's currently selected task, or null when nothing is
    /// selected or the mode has no notion of a single selected task. Drives
    /// the shell's cross-mode gestures (open the dependency tree, edit,
    /// close/reopen, delete) for whichever mode is active.
    /// </summary>
    string? SelectedTaskId => null;

    /// <summary>
    /// Asks the mode to move its selection onto the task with the given id,
    /// when it tracks one. The default no-op leaves modes that do not (or do
    /// not yet) support moving their selection unaffected.
    /// </summary>
    void SelectTask(string id)
    {
    }

    /// <summary>
    /// Hints from the active tab's <see cref="KeyDispatcher.GlobalKeyMap"/>
    /// that this mode wants hidden from the footer, because its current
    /// state makes those keys inert (for example the mail mode's Workspace
    /// mailbox refusing u/a/c/r with a toast rather than acting on them).
    /// Matched by value against <see cref="KeyHint"/> equality in
    /// <see cref="KeyDispatcher.CombineHints"/>, so a mode never needs to
    /// re-check whether the global table still carries the hint it wants
    /// gone. The default suppresses nothing.
    /// </summary>
    IReadOnlyCollection<KeyHint> SuppressedGlobalHints => [];
}
