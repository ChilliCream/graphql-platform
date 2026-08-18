using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tui.Details;
using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Shell;
using ChilliCream.Nitro.CommandLine.Tui.Theming;
using Spectre.Console.Rendering;
using CursorDirection = ChilliCream.Nitro.CommandLine.Tui.Input.CursorDirection;

namespace ChilliCream.Nitro.CommandLine.Tui.Search;

/// <summary>
/// The search mode: a query input line, a live-updating results list, and a
/// detail panel for the selected task, with focus switching between the
/// three. Queries the store through <see cref="TaskQueryParser"/> and
/// <see cref="TaskQuery.ToFilter"/>; issues no SQL of its own.
/// </summary>
/// <remarks>
/// Text typed into the query input and the debounce that follows it are not
/// reachable through <see cref="ITuiMode.Handle"/>: the shell's key dispatch
/// only ever produces the closed <see cref="TuiMessage"/> set from a
/// <see cref="Input.KeyMap"/>. <see cref="HandleQueryKey"/> and
/// <see cref="TickAsync"/> expose that behavior directly for the shell to
/// drive: raw key input while <see cref="Focus"/> is <see cref="SearchFocus.Input"/>,
/// and every tick, respectively. <see cref="FocusInput"/> is likewise driven
/// directly by the shell for the '/' gesture, since jumping into search from
/// another mode has no equivalent in the closed <see cref="TuiMessage"/> set.
/// </remarks>
internal sealed class SearchMode : ITuiMode
{
    /// <summary>
    /// How long after the last keystroke a valid query is re-run against the
    /// store.
    /// </summary>
    public static readonly TimeSpan DebounceWindow = TimeSpan.FromMilliseconds(200);

    private readonly ITaskStore _store;
    private readonly SearchResultsList _results = new();
    private readonly TaskDetailModel _detailModel;
    private readonly TaskDetailView _detailView;

    private SearchFocus _focus = SearchFocus.Input;
    private string _queryText = "";
    private int _textCursor;
    private TaskQuery _lastAppliedQuery = TaskQuery.Empty;
    private TaskQuery? _pendingQuery;
    private DateTimeOffset? _pendingQueryDueAt;

    public SearchMode(ITaskStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _pendingQuery = TaskQuery.Empty;
        _detailModel = new TaskDetailModel(store);
        _detailView = new TaskDetailView(_detailModel);
    }

    /// <summary>
    /// Which pane currently holds focus.
    /// </summary>
    public SearchFocus Focus => _focus;

    /// <summary>
    /// The current contents of the query input line.
    /// </summary>
    public string QueryText => _queryText;

    /// <summary>
    /// The current parse error for <see cref="QueryText"/>, or null when it
    /// parses successfully.
    /// </summary>
    public TaskQueryParseError? ParseError { get; private set; }

    /// <summary>
    /// The id of the selected task in the results list, or null when the
    /// list is empty. Detail and editing modes hook in through this.
    /// </summary>
    public string? SelectedTaskId => _results.SelectedTaskId;

    /// <summary>
    /// The tasks currently loaded in the results list.
    /// </summary>
    public IReadOnlyList<TaskItem> Results => _results.Tasks;

    /// <summary>
    /// Search mode's own bindings: Escape walks focus back, mirroring h, and
    /// Tab moves focus forward, mirroring Enter. Every other gesture (j/k/h/l,
    /// arrows, Enter, r) comes from the global key table.
    /// </summary>
    public KeyMap? KeyMap { get; } = new KeyMap(
    [
        new KeyBinding(
            new KeyChord(ConsoleKey.Escape, ConsoleModifiers.None, '\u001b'),
            () => new TuiMessage.MoveCursor(CursorDirection.Left)),
        new KeyBinding(
            new KeyChord(ConsoleKey.Tab, ConsoleModifiers.None, '\t'),
            () => new TuiMessage.OpenSelected())
    ]);

    public void OnEnter()
    {
        _pendingQuery = _lastAppliedQuery;
        _pendingQueryDueAt = null;
    }

    public void OnResize(int width, int height)
    {
    }

    public IReadOnlyList<TuiMessage> Handle(TuiMessage message)
    {
        switch (message)
        {
            case TuiMessage.RefreshRequested:
                _pendingQuery = _lastAppliedQuery;
                _pendingQueryDueAt = null;
                return [];

            case TuiMessage.MoveCursor(var direction):
                return HandleMoveCursor(direction);

            case TuiMessage.MoveToEdge(var edge):
                if (_focus == SearchFocus.List)
                {
                    _results.MoveToEdge(edge);
                }
                else if (_focus == SearchFocus.Detail)
                {
                    if (edge == EdgeTarget.Top)
                    {
                        _detailView.ScrollToTop();
                    }
                    else
                    {
                        _detailView.ScrollToBottom();
                    }
                }

                return [];

            case TuiMessage.OpenSelected:
                HandleOpenSelected();
                return [];

            default:
                return [];
        }
    }

    /// <summary>
    /// Moves focus to the query input, the entry point for the '/' gesture.
    /// </summary>
    public void FocusInput() => _focus = SearchFocus.Input;

    /// <summary>
    /// Handles one raw key while the query input has focus: cursor movement,
    /// insertion, and deletion. Every edit re-parses <see cref="QueryText"/>
    /// via <see cref="TaskQueryParser"/>; a valid parse schedules a query
    /// <see cref="DebounceWindow"/> after <paramref name="now"/>, an invalid
    /// one sets <see cref="ParseError"/> and schedules nothing. Has no effect
    /// when a pane other than the input has focus.
    /// </summary>
    public void HandleQueryKey(ConsoleKeyInfo info, DateTimeOffset now)
    {
        if (_focus != SearchFocus.Input)
        {
            return;
        }

        switch (info.Key)
        {
            case ConsoleKey.Backspace:
                if (_textCursor == 0)
                {
                    return;
                }

                _queryText = _queryText.Remove(_textCursor - 1, 1);
                _textCursor--;
                break;

            case ConsoleKey.Delete:
                if (_textCursor >= _queryText.Length)
                {
                    return;
                }

                _queryText = _queryText.Remove(_textCursor, 1);
                break;

            case ConsoleKey.LeftArrow:
                _textCursor = Math.Max(0, _textCursor - 1);
                return;

            case ConsoleKey.RightArrow:
                _textCursor = Math.Min(_queryText.Length, _textCursor + 1);
                return;

            case ConsoleKey.Home:
                _textCursor = 0;
                return;

            case ConsoleKey.End:
                _textCursor = _queryText.Length;
                return;

            default:
                if (char.IsControl(info.KeyChar))
                {
                    return;
                }

                _queryText = _queryText.Insert(_textCursor, info.KeyChar.ToString());
                _textCursor++;
                break;
        }

        OnQueryTextChanged(now);
    }

    /// <summary>
    /// Runs the query pending from a debounced edit, a manual refresh, or
    /// mode entry, once <paramref name="now"/> reaches its due time. Returns
    /// whether a query ran.
    /// </summary>
    public async Task<bool> TickAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (_pendingQuery is not { } query)
        {
            return false;
        }

        if (_pendingQueryDueAt is { } dueAt && now < dueAt)
        {
            return false;
        }

        _pendingQuery = null;
        _pendingQueryDueAt = null;

        await RunQueryAsync(query, now, cancellationToken).ConfigureAwait(false);
        return true;
    }

    public IRenderable Render(int width, int height)
    {
        var inputArea = RenderInput();
        var inputHeight = ParseError is null ? 1 : 2;
        var bodyHeight = Math.Max(0, height - inputHeight);

        var listWidth = Math.Max(1, width * 3 / 5);
        var detailWidth = Math.Max(1, width - listWidth);

        var body = new Layout("body").SplitColumns(
            new Layout("list", RenderListPane(listWidth, bodyHeight)),
            new Layout("detail", RenderDetailPane(detailWidth, bodyHeight)));

        return new Layout("root").SplitRows(
            new Layout("input", inputArea).Size(inputHeight),
            body);
    }

    private IReadOnlyList<TuiMessage> HandleMoveCursor(CursorDirection direction)
    {
        switch (direction)
        {
            case CursorDirection.Down:
            case CursorDirection.Up:
                if (_focus == SearchFocus.List)
                {
                    _results.MoveCursor(direction);
                }
                else if (_focus == SearchFocus.Detail)
                {
                    if (direction == CursorDirection.Down)
                    {
                        _detailView.ScrollDown();
                    }
                    else
                    {
                        _detailView.ScrollUp();
                    }
                }

                break;

            case CursorDirection.Left:
                if (_focus == SearchFocus.Input)
                {
                    // Already at the leftmost pane: h/Escape leaves search
                    // mode instead of doing nothing.
                    return [new TuiMessage.Back()];
                }

                _focus = _focus switch
                {
                    SearchFocus.Detail => SearchFocus.List,
                    SearchFocus.List => SearchFocus.Input,
                    _ => _focus
                };

                break;

            case CursorDirection.Right:
                if (_focus == SearchFocus.List)
                {
                    _focus = SearchFocus.Detail;
                }

                break;
        }

        return [];
    }

    private void HandleOpenSelected()
    {
        _focus = _focus switch
        {
            SearchFocus.Input => SearchFocus.List,
            SearchFocus.List => SearchFocus.Detail,
            _ => _focus
        };
    }

    private void OnQueryTextChanged(DateTimeOffset now)
    {
        if (TaskQueryParser.TryParse(_queryText, out var query, out var error))
        {
            ParseError = null;
            _pendingQuery = query;
            _pendingQueryDueAt = now + DebounceWindow;
        }
        else
        {
            ParseError = error;
            _pendingQuery = null;
            _pendingQueryDueAt = null;
        }
    }

    private async Task RunQueryAsync(TaskQuery query, DateTimeOffset now, CancellationToken cancellationToken)
    {
        _lastAppliedQuery = query;
        var filter = query.ToFilter(now);
        var tasks = await _store.QueryTasksAsync(filter, cancellationToken).ConfigureAwait(false);
        _results.SetTasks(tasks);
    }

    private IRenderable RenderInput()
    {
        const string prefix = "> ";
        var content = _focus == SearchFocus.Input
            ? RenderWithCursor(_queryText, _textCursor)
            : Markup.Escape(_queryText);

        var line = new Markup(Markup.Escape(prefix) + content);

        if (ParseError is not { } error)
        {
            return line;
        }

        var errorStyle = ThemeTokens.GetStyle("toast.error.border").ToMarkup();
        var hint = new Markup($"[{errorStyle}]{Markup.Escape(error.Message)}[/]");
        return new Rows(line, hint);
    }

    private static string RenderWithCursor(string text, int cursor)
    {
        var clampedCursor = Math.Clamp(cursor, 0, text.Length);
        var before = Markup.Escape(text[..clampedCursor]);
        var atCursor = clampedCursor < text.Length ? text[clampedCursor].ToString() : " ";
        var after = clampedCursor < text.Length ? Markup.Escape(text[(clampedCursor + 1)..]) : string.Empty;

        return $"{before}[black on white]{Markup.Escape(atCursor)}[/]{after}";
    }

    private IRenderable RenderListPane(int width, int height)
    {
        var focused = _focus == SearchFocus.List;
        var innerWidth = Math.Max(1, width - 4);
        var innerHeight = Math.Max(1, height - 2);
        var content = _results.Render(innerWidth, innerHeight, focused);
        var borderToken = focused ? "board.column.border.focused" : "board.column.border";

        return new Panel(content)
        {
            Header = new PanelHeader("Results"),
            Border = BoxBorder.Rounded,
            BorderStyle = ThemeTokens.GetStyle(borderToken)
        };
    }

    private IRenderable RenderDetailPane(int width, int height)
    {
        var focused = _focus == SearchFocus.Detail;

        if (SelectedTaskId is not { } id)
        {
            var borderToken = focused ? "board.column.border.focused" : "board.column.border";

            return new Panel(new Markup(Markup.Escape("No task selected.")))
            {
                Header = new PanelHeader("Detail"),
                Border = BoxBorder.Rounded,
                BorderStyle = ThemeTokens.GetStyle(borderToken)
            };
        }

        if (_detailModel.CurrentTaskId != id)
        {
            _detailModel.LoadAsync(id, CancellationToken.None).GetAwaiter().GetResult();
        }

        return _detailView.Render(width, height, focused);
    }
}
