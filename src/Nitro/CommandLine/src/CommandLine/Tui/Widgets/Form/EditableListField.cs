using Spectre.Console.Rendering;

namespace ChilliCream.Nitro.CommandLine.Tui.Widgets.Form;

/// <summary>
/// An editable list of string entries, used for labels: add, remove, and edit
/// entries in place. While browsing, up and down move the selected entry and
/// are left unconsumed at the first or last entry so focus can move on; while
/// editing an entry, Escape cancels only that edit rather than the whole form.
/// </summary>
internal sealed class EditableListField : FormField
{
    private const string SelectedRowStyle = "aqua";
    private const string Bullet = "- ";

    private readonly List<string> _entries;

    private int _selectedIndex;
    private LineEditor? _editor;
    private string? _editingOriginalText;
    private bool _isEditingNewEntry;

    public EditableListField(
        string id,
        string label,
        bool required = false,
        IEnumerable<string>? initialValues = null,
        Func<FormValue, string?>? validator = null)
        : base(id, label, required, validator)
    {
        _entries = initialValues is null ? [] : [.. initialValues];
        _selectedIndex = _entries.Count == 0 ? -1 : 0;
    }

    private bool IsEditing => _editor is not null;

    public override FormValue GetValue() => new FormValue.List([.. _entries]);

    public override bool HandleKey(ConsoleKeyInfo info)
    {
        if (IsEditing)
        {
            return HandleEditingKey(info);
        }

        switch (info.Key)
        {
            case ConsoleKey.UpArrow:
                if (_selectedIndex <= 0)
                {
                    return false;
                }

                _selectedIndex--;
                return true;

            case ConsoleKey.DownArrow:
                if (_selectedIndex < 0 || _selectedIndex >= _entries.Count - 1)
                {
                    return false;
                }

                _selectedIndex++;
                return true;

            case ConsoleKey.Enter:
                if (_selectedIndex < 0)
                {
                    return false;
                }

                BeginEditing(_entries[_selectedIndex], isNewEntry: false);
                return true;

            case ConsoleKey.Delete:
                RemoveSelected();
                return true;

            default:
                if (info.KeyChar == 'a')
                {
                    AddEntry();
                    return true;
                }

                if (info.KeyChar == 'd')
                {
                    RemoveSelected();
                    return true;
                }

                return false;
        }
    }

    public override IRenderable Render(int width, bool focused)
    {
        if (_entries.Count == 0)
        {
            return RenderPanel(new Markup(RenderPlaceholder("no labels - type to add")), width, focused);
        }

        var rows = new List<IRenderable>();

        for (var i = 0; i < _entries.Count; i++)
        {
            var isCurrentRow = focused && i == _selectedIndex;
            var text = IsEditing && i == _selectedIndex
                ? RenderEditingLine(_editor!)
                : Markup.Escape(_entries[i]);

            var line = Bullet + text;

            if (isCurrentRow)
            {
                line = $"[{SelectedRowStyle}]{line}[/]";
            }

            rows.Add(new Markup(line));
        }

        return RenderPanel(new Rows(rows), width, focused);
    }

    private bool HandleEditingKey(ConsoleKeyInfo info)
    {
        switch (info.Key)
        {
            case ConsoleKey.Enter:
                CommitEditing();
                return true;

            case ConsoleKey.Escape:
                CancelEditing();
                return true;

            case ConsoleKey.UpArrow:
            case ConsoleKey.DownArrow:
                // Swallowed so browsing the list cannot happen mid-edit.
                return true;

            default:
                if (_editor!.HandleKey(info))
                {
                    return true;
                }

                // An unrecognized key (for example Tab) blurs the entry: commit
                // it in place and let the key fall through as focus traversal.
                CommitEditing();
                return false;
        }
    }

    private void BeginEditing(string text, bool isNewEntry)
    {
        _editor = new LineEditor(text);
        _editingOriginalText = text;
        _isEditingNewEntry = isNewEntry;
    }

    private void CommitEditing()
    {
        var text = _editor!.Text;

        if (text.Length == 0)
        {
            _entries.RemoveAt(_selectedIndex);
            _selectedIndex = Math.Min(_selectedIndex, _entries.Count - 1);
        }
        else
        {
            _entries[_selectedIndex] = text;
        }

        StopEditing();
    }

    private void CancelEditing()
    {
        if (_isEditingNewEntry)
        {
            _entries.RemoveAt(_selectedIndex);
            _selectedIndex = Math.Min(_selectedIndex, _entries.Count - 1);
        }
        else
        {
            _entries[_selectedIndex] = _editingOriginalText!;
        }

        StopEditing();
    }

    private void StopEditing()
    {
        _editor = null;
        _editingOriginalText = null;
        _isEditingNewEntry = false;
    }

    private void AddEntry()
    {
        var insertAt = _selectedIndex + 1;
        _entries.Insert(insertAt, string.Empty);
        _selectedIndex = insertAt;
        BeginEditing(string.Empty, isNewEntry: true);
    }

    private void RemoveSelected()
    {
        if (_selectedIndex < 0)
        {
            return;
        }

        _entries.RemoveAt(_selectedIndex);
        _selectedIndex = Math.Min(_selectedIndex, _entries.Count - 1);
    }

    private static string RenderEditingLine(LineEditor editor)
    {
        const string cursorStyle = "black on white";

        var text = editor.Text;
        var cursor = editor.Cursor;

        var before = Markup.Escape(text[..cursor]);
        var atCursor = cursor < text.Length ? text[cursor].ToString() : " ";
        var after = cursor < text.Length ? Markup.Escape(text[(cursor + 1)..]) : string.Empty;

        return $"{before}[{cursorStyle}]{Markup.Escape(atCursor)}[/]{after}";
    }
}
