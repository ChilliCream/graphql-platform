namespace ChilliCream.Nitro.CommandLine.Cloud;

internal class PagedSelectionPrompt<TEdge>
{
    private readonly IPaginationContainer<TEdge> _container;

    private Func<TEdge, string> _converter = e => e?.ToString() ?? string.Empty;

    private string _title = "Select on item";

    public PagedSelectionPrompt(IPaginationContainer<TEdge> container)
    {
        _container = container;
    }

    public PagedSelectionPrompt<TEdge> UseConverter(Func<TEdge, string> converter)
    {
        _converter = converter;
        return this;
    }

    public PagedSelectionPrompt<TEdge> Title(string title)
    {
        _title = title;
        return this;
    }

    public async Task<TEdge?> RenderAsync(
        IAnsiConsole console,
        CancellationToken cancellationToken)
    {
        TEdge? selectedEdge = default;

        var data = await _container.GetCurrentAsync(cancellationToken);
        if (data.Count == 0)
        {
            throw new ExitException("No items found");
        }

        while (selectedEdge is null && !cancellationToken.IsCancellationRequested)
        {
            var choices = data
                .Select(x => new SelectionItem(x, _converter(x)))
                .AsEnumerable();

            var loadNext = new SelectionItem(default!, "[dim italic]Load Next...[/]");
            var loadPrevious = new SelectionItem(default!, "[dim italic]Load Previous..[/]");

            if (_container.HasNext())
            {
                choices = choices.Append(loadNext);
            }

            if (_container.HasPrevious())
            {
                choices = choices.Prepend(loadPrevious);
            }

            var selected = await new SelectionPrompt<SelectionItem>()
                .Title(_title)
                .PageSize(10)
                .UseConverter(e => e.DisplayName)
                .AddChoices(choices)
                .ShowAsync(console, cancellationToken);

            if (selected == loadNext)
            {
                data = await _container.FetchNextAsync(cancellationToken);
            }
            else if (selected == loadPrevious)
            {
                data = await _container.FetchPreviousAsync(cancellationToken);
            }
            else
            {
                selectedEdge = selected.Edge;
            }
        }

        return selectedEdge;
    }

    private readonly record struct SelectionItem(TEdge Edge, string DisplayName);
}

internal static class PagedSelectionPrompt
{
    public static PagedSelectionPrompt<TEdge> New<TEdge>(IPaginationContainer<TEdge> container)
    {
        return new PagedSelectionPrompt<TEdge>(container);
    }
}
