using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Runtime;
using Spectre.Console.Rendering;

namespace ChilliCream.Nitro.CommandLine.Tui.Shell;

/// <summary>
/// The root handler and renderer for <see cref="TuiApplication"/>. Owns the active
/// mode and composites the shell-level overlays (the quit confirmation and the
/// toast row) over it.
/// </summary>
internal sealed class TuiShell
{
    private const string QuitConfirmMessage = "Quit? (y/n)";
    private const int StatusRowHeight = 1;

    private readonly KeyDispatcher _dispatcher;
    private readonly ITuiMode _activeMode;
    private readonly Toaster _toaster = new();

    private ConfirmDialog? _confirmDialog;
    private int _width;
    private int _height;

    public TuiShell(KeyDispatcher dispatcher, ITuiMode activeMode, int initialWidth, int initialHeight)
    {
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _activeMode = activeMode ?? throw new ArgumentNullException(nameof(activeMode));
        _width = initialWidth;
        _height = initialHeight;
        _activeMode.OnEnter();
    }

    /// <summary>
    /// Raised once a pending quit is confirmed. The caller is expected to stop the
    /// <see cref="TuiApplication"/> event loop in response.
    /// </summary>
    public event Action? QuitConfirmed;

    /// <summary>
    /// Handles one <see cref="TuiEvent"/>, returning whether the frame needs to be
    /// repainted. Matches the <see cref="TuiEventHandler"/> shape expected by
    /// <see cref="TuiApplication.RunAsync"/>.
    /// </summary>
    public bool Handle(TuiEvent tuiEvent) => tuiEvent switch
    {
        TuiEvent.KeyEvent keyEvent => HandleKey(keyEvent.Info),
        TuiEvent.ResizeEvent resize => HandleResize(resize.Width, resize.Height),
        TuiEvent.TickEvent tick => _toaster.Tick(tick.Now),
        TuiEvent.DataChangedEvent => HandleMessage(new TuiMessage.RefreshRequested()),
        _ => false
    };

    /// <summary>
    /// Renders the current frame: the confirm dialog or the active mode filling the
    /// content region, with a reserved bottom row for the current toast. Matches the
    /// <see cref="TuiFrameRenderer"/> shape expected by <see cref="TuiApplication.RunAsync"/>.
    /// </summary>
    public IRenderable Render()
    {
        var contentHeight = Math.Max(0, _height - StatusRowHeight);

        var content = _confirmDialog is { } dialog
            ? dialog.Render(_width, contentHeight)
            : _activeMode.Render(_width, contentHeight);

        var toastRow = _toaster.Render() ?? (IRenderable)new Markup(string.Empty);

        return new Layout("root").SplitRows(
            new Layout("content", content),
            new Layout("status", toastRow).Size(StatusRowHeight));
    }

    private bool HandleResize(int width, int height)
    {
        _width = width;
        _height = height;
        _activeMode.OnResize(width, Math.Max(0, height - StatusRowHeight));
        return true;
    }

    private bool HandleKey(ConsoleKeyInfo info)
    {
        // While the confirm dialog is active it gets key priority; otherwise the
        // active mode does. Either way, keys unresolved by that table still fall
        // back to the global table inside the dispatcher.
        var priorityKeyMap = _confirmDialog?.KeyMap ?? _activeMode.KeyMap;
        var message = _dispatcher.Dispatch(info, priorityKeyMap);

        return message is not null && HandleMessage(message);
    }

    private bool HandleMessage(TuiMessage message)
    {
        switch (message)
        {
            case TuiMessage.QuitRequested:
                _confirmDialog = new ConfirmDialog(QuitConfirmMessage);
                return true;

            case TuiMessage.ConfirmQuit:
                _confirmDialog = null;
                QuitConfirmed?.Invoke();
                return true;

            case TuiMessage.CancelQuit:
                _confirmDialog = null;
                return true;

            case TuiMessage.ShowToast showToast:
                _toaster.Enqueue(showToast.Text, showToast.Style, DateTimeOffset.UtcNow);
                return true;

            default:
                foreach (var followUp in _activeMode.Handle(message))
                {
                    HandleMessage(followUp);
                }

                return true;
        }
    }
}
