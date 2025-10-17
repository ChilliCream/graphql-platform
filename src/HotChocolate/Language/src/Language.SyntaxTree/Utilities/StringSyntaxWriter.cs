using System.Text;

namespace HotChocolate.Language.Utilities;

public class StringSyntaxWriter : ISyntaxWriter
{
    private static readonly StringSyntaxWriterPool s_pool = new();
    private int _indent;

    public static StringSyntaxWriter Rent()
    {
        return s_pool.Get();
    }

    public static void Return(StringSyntaxWriter writer)
    {
        s_pool.Return(writer);
    }

    internal StringBuilder StringBuilder { get; } = new();

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
    }

    public void Write(string s)
    {
        StringBuilder.Append(s);
    }

    public void WriteIndent(bool condition = true)
    {
        if (condition && _indent > 0)
        {
            StringBuilder.Append(' ', 2 * _indent);
        }
    }

    public void WriteLine(bool condition = true)
    {
        if (condition)
        {
            StringBuilder.AppendLine();
        }
    }

    public void WriteSpace(bool condition = true)
    {
        if (condition)
        {
            StringBuilder.Append(' ');
        }
    }

    public void Clear()
    {
        StringBuilder.Clear();
    }

    public override string ToString()
    {
        return StringBuilder.ToString();
    }
}
