using System.Text;

namespace HotChocolate.Fusion.Language;

internal class StringSyntaxWriter(StringSyntaxWriterOptions? options = null) : ISyntaxWriter
{
    private readonly StringSyntaxWriterOptions _options = options
        ?? new StringSyntaxWriterOptions();

    private int _indent;
    private int _column;
    private readonly StringBuilder _stringBuilder = new();

    /// <inheritdoc />
    public int Column => _column;

    public void Indent()
    {
        _indent++;
    }

    public void Unindent()
    {
        if (_indent > 0)
        {
            _indent--;
        }
    }

    public void Write(char c)
    {
        _stringBuilder.Append(c);

        if (c == '\n')
        {
            _column = 0;
        }
        else
        {
            _column++;
        }
    }

    public void Write(string s)
    {
        _stringBuilder.Append(s);

        var lastNewLine = s.LastIndexOf('\n');

        if (lastNewLine >= 0)
        {
            _column = s.Length - lastNewLine - 1;
        }
        else
        {
            _column += s.Length;
        }
    }

    public void WriteIndent(bool condition = true)
    {
        if (condition && _indent > 0)
        {
            var spaces = _options.IndentSize * _indent;
            _stringBuilder.Append(' ', spaces);
            _column += spaces;
        }
    }

    public void WriteLine(bool condition = true)
    {
        if (condition)
        {
            _stringBuilder.Append(_options.NewLine);
            _column = 0;
        }
    }

    public void WriteSpace(bool condition = true)
    {
        if (condition)
        {
            _stringBuilder.Append(' ');
            _column++;
        }
    }

    public void Clear()
    {
        _stringBuilder.Clear();
        _column = 0;
    }

    public override string ToString()
    {
        return _stringBuilder.ToString();
    }
}
