using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tui.Details;
using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Shell;
using Spectre.Console.Rendering;
using CursorDirection = ChilliCream.Nitro.CommandLine.Tui.Input.CursorDirection;

namespace ChilliCream.Nitro.CommandLine.Tui.Board;

/// <summary>
/// Displays the task loaded by <see cref="OpenOnTask"/> as a scrollable detail
/// view with a metadata sidebar.
/// </summary>
internal sealed class BoardDetailMode : ITuiMode
{
    private readonly TaskDetailModel _model;
    private readonly TaskDetailView _view;

    public BoardDetailMode(ITaskStore store)
    {
        ArgumentNullException.ThrowIfNull(store);

        _model = new TaskDetailModel(store);
        _view = new TaskDetailView(_model);
    }

    /// <summary>
    /// Binds Escape to leave the detail mode with a matching footer hint.
    /// </summary>
    public KeyMap? KeyMap { get; } = new KeyMap(
    [
        new KeyBinding(
            new KeyChord(ConsoleKey.Escape, ConsoleModifiers.None, ''),
            () => new TuiMessage.Back(),
            new KeyHint("esc", "back"))
    ]);

    /// <inheritdoc />
    public string? SelectedTaskId => _model.CurrentTaskId;

    /// <inheritdoc />
    public void OnEnter()
    {
    }

    /// <inheritdoc />
    public void OnResize(int width, int height)
    {
    }

    /// <inheritdoc />
    public IReadOnlyList<TuiMessage> Handle(TuiMessage message)
    {
        switch (message)
        {
            case TuiMessage.MoveCursor(CursorDirection.Down):
                _view.ScrollDown();
                return [];

            case TuiMessage.MoveCursor(CursorDirection.Up):
                _view.ScrollUp();
                return [];

            case TuiMessage.MoveToEdge(EdgeTarget.Top):
                _view.ScrollToTop();
                return [];

            case TuiMessage.MoveToEdge(EdgeTarget.Bottom):
                _view.ScrollToBottom();
                return [];

            case TuiMessage.RefreshRequested:
                Reload();
                return [];

            default:
                return [];
        }
    }

    /// <summary>
    /// Loads <paramref name="taskId"/> through the store, replacing whichever
    /// task was previously loaded.
    /// </summary>
    public void OpenOnTask(string taskId)
    {
        ArgumentNullException.ThrowIfNull(taskId);
        _model.LoadAsync(taskId, CancellationToken.None).GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    public IRenderable Render(int width, int height) => _view.Render(width, height, focused: true);

    private void Reload()
    {
        if (_model.CurrentTaskId is { } id)
        {
            _model.LoadAsync(id, CancellationToken.None).GetAwaiter().GetResult();
        }
    }
}
