using Spectre.Console.Rendering;

namespace ChilliCream.Nitro.CLI;

internal class SelectableTable<TEdge>
{
    private readonly List<CustomTableColumn> _columns = new();
    private readonly List<Action<Table>> _configureTable = new();

    private readonly Dictionary<ConsoleKeyOrChar,
            Func<SelectableTable<TEdge>, int, CancellationToken, Task<InputAction>>>
        _keyActions = new();

    private readonly List<Func<SelectableTable<TEdge>, CustomMarkup>> _addons = new();

    private string _title = string.Empty;

    public SelectableTable(IReadOnlyList<TEdge> items)
    {
        Items = items;

        AddKeyAction(ConsoleKey.UpArrow,
            (_, index) => new InputAction.Next(index > 0 ? index - 1 : index));
        AddKeyAction(ConsoleKey.DownArrow,
            (_, index)
                => new InputAction.Next(index < SelectableItemsCount - 1 ? index + 1 : index));

        AddKeyAction(ConsoleKey.Enter,
            (_, index) => index >= Items.Count
                ? new InputAction.None()
                : new InputAction.Select(index));

        AddKeyAction(ConsoleKey.Escape, (_, _) => new InputAction.Break());
        AddKeyAction(ConsoleKey.Home, (_, _) => new InputAction.Next(0));

        AddKeyAction(ConsoleKey.End,
            (_, _) => new InputAction.Next(SelectableItemsCount - 1));

        AddKeyAction(ConsoleKey.Tab,
            (_, index) => index < SelectableItemsCount - 1
                ? new InputAction.Next(index + 1)
                : new InputAction.Next(index));
    }

    public int SelectableItemsCount
        => Items.Count + _addons.Count(x => x(this).IsSelectable);

    public IReadOnlyList<TEdge> Items { get; set; }

    public int SelectedIndex { get; set; }

    public Dictionary<string, object> ContextData { get; set; } = new();

    public SelectableTable<TEdge> Title(string title)
    {
        _title = title;
        return this;
    }

    public SelectableTable<TEdge> AddColumn(
        string name,
        Func<TEdge, string> select,
        Action<TableColumn>? configure = null)
    {
        _columns.Add(new CustomTableColumn(name, configure, select));
        return this;
    }

    public SelectableTable<TEdge> AddKeyAction(
        ConsoleKey key,
        Func<SelectableTable<TEdge>, int, CancellationToken, Task<InputAction>> action)
    {
        _keyActions.Add(key, action);
        return this;
    }

    public SelectableTable<TEdge> AddKeyAction(
        char key,
        Func<SelectableTable<TEdge>, int, CancellationToken, Task<InputAction>> action)
    {
        _keyActions.Add(key, action);
        return this;
    }

    public SelectableTable<TEdge> AddKeyAction(
        ConsoleKey key,
        Func<SelectableTable<TEdge>, int, InputAction> action)
    {
        _keyActions[key] = (_, index, _) => Task.FromResult(action(this, index));
        return this;
    }

    public SelectableTable<TEdge> AddKeyAction(
        char key,
        Func<SelectableTable<TEdge>, int, InputAction> action)
    {
        _keyActions.Add(key, (_, index, _) => Task.FromResult(action(this, index)));
        return this;
    }

    public SelectableTable<TEdge> AddAddon(Func<SelectableTable<TEdge>, CustomMarkup> addonFactory)
    {
        _addons.Add(addonFactory);
        return this;
    }

    private Table CreateTable()
    {
        var table = new Table();
        _configureTable.ForEach(apply => apply(table));
        _columns.ForEach(c => table.AddColumn(c.Name, c.Configure));
        return table;
    }

    private IEnumerable<string> CreateRow(TEdge edge) => _columns.Select(c => c.Select(edge));

    public virtual async Task<TEdge?> RenderAsync(
        IAnsiConsole console,
        CancellationToken cancellationToken)
    {
        return await console
            .Live(Text.Empty)
            .Overflow(VerticalOverflow.Visible)
            .AutoClear(true)
            .StartAsync(ctx => RenderTableAsync(console, ctx, cancellationToken));
    }

    protected virtual async Task<TEdge?> RenderTableAsync(
        IAnsiConsole console,
        LiveDisplayContext ctx,
        CancellationToken cancellationToken)
    {
        var title = new Padder(
            new Text(_title, new Style(decoration: Decoration.Bold)).Centered(),
            new Padding(0, 1));

        if (Items.Count == 0)
        {
            ctx.UpdateTarget(new Rows(
                title,
                new Text("There was no data found.").Justify(Justify.Center)));

            await console.Input.ReadKeyAsync(true, cancellationToken);
            return default;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            var addons = _addons.Select(f => f(this)).ToArray();
            var columns = new List<IRenderable>();
            var selectableAddonIndex = 0;
            for (var i = 0; i < addons.Length; i++)
            {
                var addon = addons[i];
                IRenderable column;
                if (addon.IsSelectable)
                {
                    column = SelectedIndex == selectableAddonIndex + Items.Count
                        ? addon.SelectedContent
                        : addon.Content;

                    selectableAddonIndex++;
                }
                else
                {
                    column = addon.Content;
                }

                if (!addon.IsHidden)
                {
                    columns.Add(column);
                }
            }

            ctx.UpdateTarget(new Rows(
                title,
                CreateTable()
                    .Centered()
                    .AddRows(SelectedIndex, Items.Select(CreateRow)),
                Align.Center(new Rows(columns))));

            ctx.Refresh();

            if (await console.Input.ReadKeyAsync(true, cancellationToken) is not { } rawKey)
            {
                continue;
            }

            InputAction? inputAction = null;

            if (SelectedIndex > Items.Count - 1)
            {
                var selectedAddon =
                    addons.Where(x => x.IsSelectable).ToArray()[SelectedIndex - Items.Count];

                if (selectedAddon.HandleInput is not null)
                {
                    inputAction = await selectedAddon.HandleInput(rawKey);
                }
            }

            if (inputAction is null && (
                    _keyActions.TryGetValue(rawKey.Key, out var action) ||
                    _keyActions.TryGetValue(rawKey.KeyChar, out action)))
            {
                inputAction = await action(this, SelectedIndex, cancellationToken);
            }

            switch (inputAction)
            {
                case InputAction.Next next:
                    SelectedIndex = next.Index;
                    break;

                case InputAction.Select select:
                    return Items[select.Index];

                case InputAction.Break:
                    return default;

                case InputAction.None:
                    continue;

                default:
                    continue;
            }
        }

        return default;
    }

    private struct CustomTableColumn
    {
        public CustomTableColumn(
            string name,
            Action<TableColumn>? configure,
            Func<TEdge, string> select)
        {
            Configure = configure;
            Name = name;
            Select = select;
        }

        public Action<TableColumn>? Configure { get; }

        public string Name { get; }

        public Func<TEdge, string> Select { get; }
    }

    private record struct ConsoleKeyOrChar(char Char, ConsoleKey Key)
    {
        public static implicit operator ConsoleKeyOrChar(ConsoleKey key) => new('\0', key);

        public static implicit operator ConsoleKeyOrChar(char c) => new(c, default);
    }
}

internal record InputAction
{
    public record Select(int Index) : InputAction;

    public record Next(int Index) : InputAction;

    public record Break : InputAction;

    public record None : InputAction;
}

internal struct CustomMarkup
{
    public Renderable Content { get; init; }

    public Renderable SelectedContent { get; init; }

    public bool IsSelectable { get; init; }

    public bool IsHidden { get; init; }

    public Func<ConsoleKeyInfo, Task<InputAction?>>? HandleInput { get; init; }

    public static readonly CustomMarkup Hidden = new()
    {
        Content = Text.Empty,
        SelectedContent = Text.Empty,
        IsSelectable = false,
        IsHidden = true,
        HandleInput = null
    };
}

internal static class SelectableTable
{
    public static SelectableTable<TEdge> From<TEdge>(IReadOnlyList<TEdge> container)
        => new(container);
}

internal static class SelectableTableExtension
{
    public static SelectableTable<TEdge> AddSelectableAddon<TEdge>(
        this SelectableTable<TEdge> table,
        string text,
        Action handleSelect)
    {
        return table.AddAddon(_ => new CustomMarkup
        {
            Content = new Markup($"[green]{text}[/]"),
            SelectedContent = new Markup($"[green underline bold]{text}[/]"),
            IsSelectable = true,
            HandleInput = key =>
            {
                if (key is not { Key: ConsoleKey.Enter })
                {
                    return Task.FromResult<InputAction?>(null);
                }

                handleSelect();

                return Task.FromResult<InputAction?>(new InputAction.Break());
            }
        });
    }

    public static SelectableTable<TEdge> AddFooterAddon<TEdge>(
        this SelectableTable<TEdge> table,
        string text)
    {
        return table.AddAddon(_ => new CustomMarkup()
        {
            Content = new Markup($"[grey dim]{text}[/]"), IsSelectable = false
        });
    }
}
