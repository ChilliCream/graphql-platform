namespace ChilliCream.Nitro.CommandLine.Cloud;

internal class PagedTable<TEdge> : SelectableTable<TEdge>
{
    private readonly IPaginationContainer<TEdge> _container;

    public PagedTable(IPaginationContainer<TEdge> container) : base(Array.Empty<TEdge>())
    {
        _container = container;
        AddKeyAction('n',
            async (_, _, ct) => await NextPageAsync(ct));

        AddKeyAction('p',
            async (_, _, ct) => await PreviousPageAsync(ct));

        AddAddon(PreviousPageAddon);
        AddAddon(NextPageAddon);
    }

    private async Task<InputAction> PreviousPageAsync(CancellationToken ct)
    {
        if (!_container.HasPrevious())
        {
            return new InputAction.None();
        }

        Items = await _container.FetchPreviousAsync(ct);

        return new InputAction.Next(0);
    }

    private async Task<InputAction> NextPageAsync(CancellationToken ct)
    {
        if (!_container.HasNext())
        {
            return new InputAction.None();
        }

        Items = await _container.FetchNextAsync(ct);
        return new InputAction.Next(0);
    }

    private CustomMarkup PreviousPageAddon(SelectableTable<TEdge> _)
    {
        if (_container.HasPrevious())
        {
            return new CustomMarkup
            {
                SelectedContent = new Markup("[green bold underline](p)revious page[/]"),
                Content = new Markup("[green](p)revious page[/]"),
                IsSelectable = true,
                HandleInput = async keyInfo => keyInfo is not { Key: ConsoleKey.Enter }
                    ? null
                    : await PreviousPageAsync(CancellationToken.None)
            };
        }

        return CustomMarkup.Hidden;
    }

    private CustomMarkup NextPageAddon(SelectableTable<TEdge> _)
    {
        if (_container.HasNext())
        {
            return new CustomMarkup
            {
                SelectedContent = new Markup("[green bold underline](n)ext page[/]"),
                Content = new Markup("[green bold](n)ext page[/]"),
                IsSelectable = true,
                HandleInput = async keyInfo => keyInfo is not { Key: ConsoleKey.Enter }
                    ? null
                    : await NextPageAsync(CancellationToken.None)
            };
        }

        return CustomMarkup.Hidden;
    }

    /// <inheritdoc />
    protected override async Task<TEdge?> RenderTableAsync(
        IAnsiConsole console,
        LiveDisplayContext ctx,
        CancellationToken cancellationToken)
    {
        Items = await _container.FetchNextAsync(cancellationToken);

        return await base.RenderTableAsync(console, ctx, cancellationToken);
    }
}

internal static class PagedTable
{
    public static PagedTable<TEdge> From<TEdge>(IPaginationContainer<TEdge> container)
        => new(container);
}
