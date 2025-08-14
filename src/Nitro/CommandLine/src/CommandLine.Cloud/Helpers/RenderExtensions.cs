namespace ChilliCream.Nitro.CLI;

internal static class RenderExtensions
{
    public static Table AddRows(
        this Table table,
        int selectedIndex,
        IEnumerable<IEnumerable<string>> rows)
    {
        var i = 0;
        foreach (var row in rows)
        {
            table.AddRow(row.Select(r => new Markup(r,
                i == selectedIndex
                    ? new Style(null, null, Decoration.Invert)
                    : Style.Plain)));

            i++;
        }

        return table;
    }
}
