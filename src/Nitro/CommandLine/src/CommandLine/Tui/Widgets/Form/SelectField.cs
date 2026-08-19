using Spectre.Console.Rendering;

namespace ChilliCream.Nitro.CommandLine.Tui.Widgets.Form;

/// <summary>
/// One selectable choice in a <see cref="SelectField"/>.
/// </summary>
internal sealed record SelectOption(string Id, string Label);

/// <summary>
/// A radio single-select field over a fixed set of choices, rendered inline on
/// as few lines as fit the field's width, wrapping to further lines only when
/// needed. Left and right move the selection; at the first or last option the
/// key is left unconsumed so focus can move on to another field, leaving up
/// and down free for that field-to-field traversal.
/// </summary>
internal sealed class SelectField : FormField
{
    private const string SelectedMarker = "(o)";
    private const string UnselectedMarker = "( )";
    private const string SelectedRowStyle = "aqua";
    private const string OptionSeparator = "  ";
    private const int MinRenderWidth = 4;

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
            case ConsoleKey.LeftArrow:
                if (_selectedIndex == 0)
                {
                    return false;
                }

                _selectedIndex--;
                return true;

            case ConsoleKey.RightArrow:
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
        var innerWidth = Math.Max(1, width - MinRenderWidth);
        var lines = new List<List<string>>();
        var lineWidth = 0;

        for (var i = 0; i < _options.Count; i++)
        {
            var isSelected = i == _selectedIndex;
            var plainWidth = SelectedMarker.Length + 1 + _options[i].Label.Length;

            if (lines.Count == 0
                || (lineWidth > 0 && lineWidth + OptionSeparator.Length + plainWidth > innerWidth))
            {
                lines.Add([]);
                lineWidth = 0;
            }

            var marker = isSelected ? SelectedMarker : UnselectedMarker;
            var option = $"{marker} {Markup.Escape(_options[i].Label)}";

            lines[^1].Add(focused && isSelected ? $"[{SelectedRowStyle}]{option}[/]" : option);
            lineWidth += (lineWidth > 0 ? OptionSeparator.Length : 0) + plainWidth;
        }

        var rows = lines
            .Select(line => (IRenderable)new Markup(string.Join(OptionSeparator, line)))
            .ToList();

        return RenderPanel(new Rows(rows), width, focused);
    }
}
