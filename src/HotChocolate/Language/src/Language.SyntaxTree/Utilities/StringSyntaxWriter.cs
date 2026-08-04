using System.Text;

namespace HotChocolate.Language.Utilities;

public class StringSyntaxWriter : ISyntaxWriter
{
    private static readonly StringSyntaxWriterPool s_pool = new();
    private int _indent;
    private int _column;

    public static StringSyntaxWriter Rent()
    {
        return s_pool.Get();
    }

    public static void Return(StringSyntaxWriter writer)
    {
        s_pool.Return(writer);
    }

    internal StringBuilder StringBuilder { get; } = new();

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
        StringBuilder.Append(c);

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
        StringBuilder.Append(s);

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
            var spaces = 2 * _indent;
            StringBuilder.Append(' ', spaces);
            _column += spaces;
        }
    }

    public void WriteLine(bool condition = true)
    {
        if (condition)
        {
            StringBuilder.AppendLine();
            _column = 0;
        }
    }

    public void WriteSpace(bool condition = true)
    {
        if (condition)
        {
            StringBuilder.Append(' ');
            _column++;
        }
    }

    public void Clear()
    {
        StringBuilder.Clear();
        _column = 0;
    }

    public override string ToString()
    {
        return StringBuilder.ToString();
    }
}
