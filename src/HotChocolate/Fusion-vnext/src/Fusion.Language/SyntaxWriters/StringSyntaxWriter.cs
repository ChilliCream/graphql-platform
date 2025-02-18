using System.Text;

namespace HotChocolate.Fusion.Language;

internal class StringSyntaxWriter(StringSyntaxWriterOptions? options = null) : ISyntaxWriter
{
    private readonly StringSyntaxWriterOptions _options = options
        ?? new StringSyntaxWriterOptions();

    private int _indent;
    private readonly StringBuilder _stringBuilder = new();

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
    }

    public void Write(string s)
    {
        _stringBuilder.Append(s);
    }

    public void WriteIndent(bool condition = true)
    {
        if (condition && _indent > 0)
        {
            _stringBuilder.Append(' ', _options.IndentSize * _indent);
        }
    }

    public void WriteLine(bool condition = true)
    {
        if (condition)
        {
            _stringBuilder.Append(_options.NewLine);
        }
    }

    public void WriteSpace(bool condition = true)
    {
        if (condition)
        {
            _stringBuilder.Append(' ');
        }
    }

    public void Clear()
    {
        _stringBuilder.Clear();
    }

    public override string ToString()
    {
        return _stringBuilder.ToString();
    }
}
