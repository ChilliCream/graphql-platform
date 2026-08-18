using ChilliCream.Nitro.CommandLine.Tui.Input;
using Spectre.Console.Rendering;

namespace ChilliCream.Nitro.CommandLine.Tui.Shell;

/// <summary>
/// A yes/no confirmation overlay. While active, it takes key priority over the
/// active mode: y confirms, n or Escape cancels.
/// </summary>
internal sealed class ConfirmDialog
{
    public ConfirmDialog(string message)
    {
        Message = message;
        KeyMap = new KeyMap(
        [
            new KeyBinding(
                new KeyChord(ConsoleKey.Y, ConsoleModifiers.None, 'y'),
                () => new TuiMessage.ConfirmQuit()),
            new KeyBinding(
                new KeyChord(ConsoleKey.N, ConsoleModifiers.None, 'n'),
                () => new TuiMessage.CancelQuit()),
            new KeyBinding(
                new KeyChord(ConsoleKey.Escape, ConsoleModifiers.None, ''),
                () => new TuiMessage.CancelQuit())
        ]);
    }

    /// <summary>
    /// The confirmation prompt shown in the dialog.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// The key table this dialog resolves while it is active.
    /// </summary>
    public KeyMap KeyMap { get; }

    /// <summary>
    /// Renders the dialog as a panel centered within the given area, replacing the
    /// mode content it covers.
    /// </summary>
    public IRenderable Render(int width, int height)
    {
        var panel = new Panel(new Markup(Markup.Escape(Message)))
        {
            Border = BoxBorder.Rounded
        };

        return new Align(panel, HorizontalAlignment.Center, VerticalAlignment.Middle)
            .Width(Math.Max(0, width))
            .Height(Math.Max(0, height));
    }
}
