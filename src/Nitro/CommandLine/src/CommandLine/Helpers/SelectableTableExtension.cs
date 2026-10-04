namespace ChilliCream.Nitro.CommandLine.Helpers;

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
        return table.AddAddon(_ => new CustomMarkup
        {
            Content = new Markup($"[grey dim]{text}[/]"),
            IsSelectable = false
        });
    }
}
