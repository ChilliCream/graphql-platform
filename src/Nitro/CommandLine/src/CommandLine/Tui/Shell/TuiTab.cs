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
    private readonly Func<string> _titleFactory;

    public TuiTab(string title, ITuiMode rootMode, KeyDispatcher dispatcher)
        : this(() => title, rootMode, dispatcher)
    {
        ArgumentNullException.ThrowIfNull(title);
    }

    public TuiTab(Func<string> titleFactory, ITuiMode rootMode, KeyDispatcher dispatcher)
    {
        _titleFactory = titleFactory ?? throw new ArgumentNullException(nameof(titleFactory));
        RootMode = rootMode ?? throw new ArgumentNullException(nameof(rootMode));
        Dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        ActiveMode = rootMode;
    }

    /// <summary>
    /// The tab's current display title, evaluated fresh on every access so a
    /// live badge (for example an unread count) stays current.
    /// </summary>
    public string Title => _titleFactory();

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
    {
        if (_modeStack.Count == 0)
        {
            return false;
        }

        ActiveMode = _modeStack.Pop();
        ActiveMode.OnResize(width, height);
        ActiveMode.OnEnter();
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
