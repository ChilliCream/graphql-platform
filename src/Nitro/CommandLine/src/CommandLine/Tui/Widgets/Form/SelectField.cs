using Spectre.Console.Rendering;

namespace ChilliCream.Nitro.CommandLine.Tui.Widgets.Form;

/// <summary>
/// One selectable choice in a <see cref="SelectField"/>.
/// </summary>
internal sealed record SelectOption(string Id, string Label);

/// <summary>
/// A radio single-select field over a fixed set of choices, rendered inline as a
/// list. Up and down move the selection; at the first or last option the key is
/// left unconsumed so focus can move on.
/// </summary>
internal sealed class SelectField : FormField
{
    private const string SelectedMarker = "(o)";
    private const string UnselectedMarker = "( )";
    private const string SelectedRowStyle = "aqua";

    private readonly IReadOnlyList<SelectOption> _options;

    private int _selectedIndex;

    public SelectField(
        string id,
        string label,
        IReadOnlyList<SelectOption> options,
        bool required = false,
        string? initialSelectedId = null,
        Func<FormValue, string?>? validator = null)
        : base(id, label, required, validator)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.Count == 0)
        {
            throw new ArgumentException("A select field must have at least one option.", nameof(options));
        }

        _options = options;

        var initialIndex = initialSelectedId is null
            ? 0
            : _options.ToList().FindIndex(o => o.Id == initialSelectedId);

        _selectedIndex = Math.Max(0, initialIndex);
    }

    public override FormValue GetValue() => new FormValue.Text(_options[_selectedIndex].Id);

    public override bool HandleKey(ConsoleKeyInfo info)
    {
        switch (info.Key)
        {
            case ConsoleKey.UpArrow:
                if (_selectedIndex == 0)
                {
                    return false;
                }

                _selectedIndex--;
                return true;

            case ConsoleKey.DownArrow:
                if (_selectedIndex == _options.Count - 1)
                {
                    return false;
                }

                _selectedIndex++;
                return true;

            default:
                return false;
        }
    }

    public override IRenderable Render(int width, bool focused)
    {
        var rows = new List<IRenderable>();

        for (var i = 0; i < _options.Count; i++)
        {
            var marker = i == _selectedIndex ? SelectedMarker : UnselectedMarker;
            var label = Markup.Escape(_options[i].Label);
            var line = $"{marker} {label}";

            if (focused && i == _selectedIndex)
            {
                line = $"[{SelectedRowStyle}]{line}[/]";
            }

            rows.Add(new Markup(line));
        }

        return RenderPanel(new Rows(rows), width, focused);
    }
}
