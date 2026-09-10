using ChilliCream.Nitro.CommandLine.Tui.Input;

namespace ChilliCream.Nitro.CommandLine.Tui.Shell;

/// <summary>
/// One tab hosted by a tabbed <see cref="TuiShell"/>: a root mode, its own
/// navigation stack, and its own <see cref="KeyDispatcher"/>. Switching tabs
/// preserves each tab's nested mode state and selection independently, and
/// <see cref="TuiMessage.Back"/> never crosses tabs since the mode stack it
/// pops belongs to whichever tab is active.
/// </summary>
internal sealed class TuiTab
{
    private readonly Stack<ITuiMode> _modeStack = new();

    public TuiTab(string title, char mnemonic, ITuiMode rootMode, KeyDispatcher dispatcher)
    {
        Title = title;
        Mnemonic = mnemonic;
        RootMode = rootMode;
        Dispatcher = dispatcher;
        ActiveMode = rootMode;
    }

    /// <summary>
    /// The tab's display title. A live count shown next to it (for example the
    /// Mail tab's unread count) comes from <see cref="RootMode"/>'s
    /// <see cref="ITuiMode.TabBadgeCount"/>.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// The letter this tab jumps to on <c>Shift+&lt;letter&gt;</c> (see
    /// <see cref="TuiShell"/>'s mnemonic resolution), and the letter
    /// bracketed in the tab strip's rendering of <see cref="Title"/>. Given
    /// explicitly rather than derived from <see cref="Title"/> so it need not
    /// be the title's first letter (for example the Memory tab's <c>e</c>).
    /// </summary>
    public char Mnemonic { get; }

    /// <summary>
    /// The mode this tab was constructed with. <see cref="ActiveMode"/> may
    /// currently be a different mode this tab navigated to.
    /// </summary>
    public ITuiMode RootMode { get; }

    /// <summary>
    /// This tab's own key dispatcher: its global key table is checked only
    /// while this tab is active, so tabs never leak key bindings into one
    /// another.
    /// </summary>
    public KeyDispatcher Dispatcher { get; }

    /// <summary>
    /// The mode currently active within this tab.
    /// </summary>
    public ITuiMode ActiveMode { get; private set; }

    /// <summary>
    /// Pushes the current mode onto this tab's navigation stack and switches
    /// to <paramref name="mode"/>. A no-op when <paramref name="mode"/> is
    /// already active.
    /// </summary>
    public void SwitchTo(ITuiMode mode, int width, int height)
    {
        if (ReferenceEquals(ActiveMode, mode))
        {
            return;
        }

        _modeStack.Push(ActiveMode);
        ActiveMode = mode;
        ActiveMode.OnResize(width, height);
        ActiveMode.OnEnter();
    }

    /// <summary>
    /// Pops this tab's navigation stack back to the previous mode. Returns
    /// <see langword="false"/> without effect when the stack is empty (the
    /// tab is already at its root).
    /// </summary>
    public bool PopMode(int width, int height)
        => PopModeCore(width, height, enterMode: true);

    /// <summary>
    /// Pops and resizes the previously suspended mode without entering it again.
    /// Returns <see langword="false"/> without effect when the stack is empty.
    /// </summary>
    public bool ResumeSuspendedMode(int width, int height)
        => PopModeCore(width, height, enterMode: false);

    private bool PopModeCore(int width, int height, bool enterMode)
    {
        if (_modeStack.Count == 0)
        {
            return false;
        }

        ActiveMode = _modeStack.Pop();
        ActiveMode.OnResize(width, height);

        if (enterMode)
        {
            ActiveMode.OnEnter();
        }

        return true;
    }

    /// <summary>
    /// Re-enters this tab's currently active mode after the shell switches
    /// tabs onto it, refreshing its layout and data.
    /// </summary>
    public void Activate(int width, int height)
    {
        ActiveMode.OnResize(width, height);
        ActiveMode.OnEnter();
    }
}
