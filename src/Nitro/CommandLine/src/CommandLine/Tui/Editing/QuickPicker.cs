using Spectre.Console.Rendering;

namespace ChilliCream.Nitro.CommandLine.Tui.Editing;

/// <summary>
/// One selectable choice in a <see cref="QuickPicker"/>: an id and the
/// pre-styled Spectre markup fragment rendered for it.
/// </summary>
internal sealed record QuickPickerOption(string Id, string Markup);

/// <summary>
/// The outcome of a completed <see cref="QuickPicker"/> interaction.
/// </summary>
internal abstract record QuickPickerResult
{
    private QuickPickerResult()
    {
    }

    /// <summary>
    /// Enter was pressed: carries the id of the option that was selected.
    /// </summary>
    public sealed record Applied(string SelectedId) : QuickPickerResult;

    /// <summary>
    /// Escape was pressed; nothing should change.
    /// </summary>
    public sealed record Cancelled : QuickPickerResult;
}

/// <summary>
/// A single-select overlay listing a fixed set of options with one
/// pre-selected: j/k or the arrow keys move the selection, Enter applies it,
/// Escape cancels. The host is expected to feed it raw key input and stop
/// showing it once <see cref="HandleKey"/> returns a non-null result.
/// </summary>
internal sealed class QuickPicker
{
    private const string SelectedMarker = "(o)";
    private const string UnselectedMarker = "( )";
    private const string SelectedRowStyle = "aqua";

    private readonly IReadOnlyList<QuickPickerOption> _options;

    private int _selectedIndex;

    public QuickPicker(string title, IReadOnlyList<QuickPickerOption> options, string? initialSelectedId = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(title);
        ArgumentNullException.ThrowIfNull(options);

        if (options.Count == 0)
        {
            throw new ArgumentException("A quick picker must have at least one option.", nameof(options));
        }

        Title = title;
        _options = options;

        var initialIndex = initialSelectedId is null
            ? 0
            : _options.ToList().FindIndex(o => o.Id == initialSelectedId);

        _selectedIndex = Math.Max(0, initialIndex);
    }

    /// <summary>
    /// The title shown above the option list.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Handles one raw key. Returns <see langword="null"/> while the picker is
    /// still active, or the terminal <see cref="QuickPickerResult"/> once the
    /// interaction ends.
    /// </summary>
    public QuickPickerResult? HandleKey(ConsoleKeyInfo info)
    {
        switch (info.Key)
        {
            case ConsoleKey.J:
            case ConsoleKey.DownArrow:
                if (_selectedIndex < _options.Count - 1)
                {
                    _selectedIndex++;
                }

                return null;

            case ConsoleKey.K:
            case ConsoleKey.UpArrow:
                if (_selectedIndex > 0)
                {
                    _selectedIndex--;
                }

                return null;

            case ConsoleKey.Enter:
                return new QuickPickerResult.Applied(_options[_selectedIndex].Id);

            case ConsoleKey.Escape:
                return new QuickPickerResult.Cancelled();

            default:
                return null;
        }
    }

    /// <summary>
    /// Renders the picker as a titled, rounded panel, centered within the
    /// given area.
    /// </summary>
    public IRenderable Render(int width, int height)
    {
        var panelWidth = Math.Clamp(width - 4, 20, 60);

        var rows = new List<IRenderable>(_options.Count);

        for (var i = 0; i < _options.Count; i++)
        {
            var marker = i == _selectedIndex ? SelectedMarker : UnselectedMarker;
            var line = $"{marker} {_options[i].Markup}";

            if (i == _selectedIndex)
            {
                line = $"[{SelectedRowStyle}]{line}[/]";
            }

            rows.Add(new Markup(line));
        }

        var panel = new Panel(new Rows(rows))
        {
            Header = new PanelHeader(Markup.Escape(Title)),
            Border = BoxBorder.Rounded,
            Width = panelWidth + 4
        };

        return new Align(panel, HorizontalAlignment.Center, VerticalAlignment.Middle)
            .Width(Math.Max(0, width))
            .Height(Math.Max(0, height));
    }
}
