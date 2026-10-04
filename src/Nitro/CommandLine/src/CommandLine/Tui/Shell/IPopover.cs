using ChilliCream.Nitro.CommandLine.Tui.Input;
using Spectre.Console.Rendering;

namespace ChilliCream.Nitro.CommandLine.Tui.Shell;

/// <summary>
/// A detail overlay opened from a mode's currently selected row through
/// <see cref="ITuiMode.TryCreatePopover"/> and hosted by <see cref="TuiShell"/> in its single
/// popover slot: one render-chain position, one raw-key route, reloaded on every data-changed
/// event, and ticked every frame.
/// </summary>
internal interface IPopover
{
    /// <summary>
    /// The current footer hints, shown in place of the active mode's own hints while the
    /// popover is open.
    /// </summary>
    IReadOnlyList<KeyHint> Hints { get; }

    /// <summary>
    /// Loads (or reloads) the popover's content, blocking the caller.
    /// </summary>
    void Load(CancellationToken cancellationToken);

    /// <summary>
    /// Recomputes any time-dependent content as of now. Returns whether anything the render
    /// depends on changed since the last call.
    /// </summary>
    bool Tick();

    /// <summary>
    /// Renders the popover into the given content area.
    /// </summary>
    IRenderable Render(int width, int height);

    /// <summary>
    /// Handles one raw key, returning the terminal <see cref="PopoverResult"/> the hosting
    /// shell is expected to act on, or null when the key was consumed without effect.
    /// </summary>
    PopoverResult? HandleKey(ConsoleKeyInfo info);
}
