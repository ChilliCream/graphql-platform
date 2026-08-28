using ChilliCream.Nitro.CommandLine.Tui.Theming;
using Spectre.Console.Rendering;

namespace ChilliCream.Nitro.CommandLine.Tui.Widgets.Form;

/// <summary>
/// The button row at the bottom of a <see cref="Form"/>: left and right move the
/// selection between buttons, rendered inline in <see cref="FormButtonSpec"/> order.
/// </summary>
internal sealed class FormButtons
{
    private readonly IReadOnlyList<FormButtonSpec> _buttons;

    private int _selectedIndex;

    public FormButtons(IReadOnlyList<FormButtonSpec> buttons)
    {
        ArgumentNullException.ThrowIfNull(buttons);

        if (buttons.Count == 0)
        {
            throw new ArgumentException("A form must have at least one button.", nameof(buttons));
        }

        _buttons = buttons;
    }

    /// <summary>
    /// The identifier of the currently selected button.
    /// </summary>
    public string SelectedId => _buttons[_selectedIndex].Id;

    /// <summary>
    /// The style of the currently selected button.
    /// </summary>
    public ButtonKind SelectedKind => _buttons[_selectedIndex].Kind;

    /// <summary>
    /// Moves the selection between buttons with left and right. Returns whether
    /// the key was consumed.
    /// </summary>
    public bool HandleKey(ConsoleKeyInfo info)
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
                if (_selectedIndex == _buttons.Count - 1)
                {
                    return false;
                }

                _selectedIndex++;
                return true;

            default:
                return false;
        }
    }

    /// <summary>
    /// Renders the button row, highlighting the selected button when the row has
    /// form focus.
    /// </summary>
    public IRenderable Render(bool focused)
    {
        var parts = new List<IRenderable>(_buttons.Count);

        for (var i = 0; i < _buttons.Count; i++)
        {
            var isSelected = focused && i == _selectedIndex;
            parts.Add(new Markup(RenderButton(_buttons[i], isSelected)));
        }

        return new Columns(parts) { Padding = new Padding(2, 0, 0, 0) };
    }

    private static string RenderButton(FormButtonSpec button, bool selected)
    {
        var tokenPrefix = button.Kind switch
        {
            ButtonKind.Primary => "form.button.primary",
            ButtonKind.Danger => "form.button.danger",
            _ => "form.button.secondary"
        };

        var style = ThemeTokens.GetStyle(selected ? $"{tokenPrefix}.focused" : tokenPrefix);

        return $"[{style.ToMarkup()}] {Markup.Escape(button.Label)} [/]";
    }
}
