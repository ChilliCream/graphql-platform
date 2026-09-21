using ChilliCream.Nitro.CommandLine.Tui.Input;
using Spectre.Console.Rendering;

namespace ChilliCream.Nitro.CommandLine.Tui.Shell;

/// <summary>
/// A mode hosted by <see cref="TuiShell"/> with key bindings, message handling,
/// and rendering.
/// </summary>
internal interface ITuiMode
{
    /// <summary>
    /// The mode-specific key table, checked before the global key table.
    /// </summary>
    KeyMap? KeyMap { get; }

    /// <summary>
    /// Called when the shell initializes the mode or activates it.
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
    /// The selected task id, or null when the mode has no selected task.
    /// The shell uses this id for task-specific actions.
    /// </summary>
    string? SelectedTaskId => null;

    /// <summary>
    /// Requests selection of the task with the given id.
    /// The default implementation does nothing.
    /// </summary>
    void SelectTask(string id)
    {
    }

    /// <summary>
    /// Global footer hints to suppress, compared by <see cref="KeyHint"/> value
    /// equality. The default suppresses no hints.
    /// </summary>
    IReadOnlyCollection<KeyHint> SuppressedGlobalHints => [];
}
