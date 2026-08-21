using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Shell;
using Spectre.Console.Rendering;
using CursorDirection = ChilliCream.Nitro.CommandLine.Tui.Input.CursorDirection;

namespace ChilliCream.Nitro.CommandLine.Tui.Agents;

/// <summary>
/// The full-screen agent detail <see cref="ITuiMode"/> the shell switches to
/// when Enter is pressed on a row in <see cref="AgentsMode"/>: renders the
/// selected agent's identity, assigned tasks, and sent mail through
/// <see cref="AgentDetailModel"/> and <see cref="AgentDetailView"/>, the same
/// model/view pair <c>BoardDetailMode</c> uses for tasks, with the body
/// scrollable via the global cursor and edge gestures.
/// </summary>
/// <remarks>
/// Opening the mode on an agent, driven directly by the shell the same way
/// <c>BoardDetailMode.OpenOnTask</c> is, is not reachable through
/// <see cref="ITuiMode.Handle"/>.
/// </remarks>
internal sealed class AgentDetailMode : ITuiMode
{
    private readonly AgentDetailModel _model;
    private readonly AgentDetailView _view;

    public AgentDetailMode(
        IAgentRegistry registry,
        ITaskStore taskStore,
        IMailStore mailStore,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(taskStore);
        ArgumentNullException.ThrowIfNull(mailStore);

        _model = new AgentDetailModel(registry, taskStore, mailStore);
        _view = new AgentDetailView(_model, timeProvider);
    }

    /// <summary>
    /// Escape's binding mirrors the global table's (leave the mode) and
    /// exists only to carry a footer hint: detail has no other bindings of
    /// its own, scroll comes from the global table.
    /// </summary>
    public KeyMap? KeyMap { get; } = new KeyMap(
    [
        new KeyBinding(
            new KeyChord(ConsoleKey.Escape, ConsoleModifiers.None, ''),
            () => new TuiMessage.Back(),
            new KeyHint("esc", "back"))
    ]);

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
    /// Loads <paramref name="name"/> through the registry, task store, and
    /// mail store, replacing whichever agent was previously loaded.
    /// </summary>
    public void OpenOnAgent(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        _model.LoadAsync(name, CancellationToken.None).GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    public IRenderable Render(int width, int height) => _view.Render(width, height, focused: true);

    private void Reload()
    {
        if (_model.CurrentAgentName is { } name)
        {
            _model.LoadAsync(name, CancellationToken.None).GetAwaiter().GetResult();
        }
    }
}
