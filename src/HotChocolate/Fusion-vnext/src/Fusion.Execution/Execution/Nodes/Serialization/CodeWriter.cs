using System.Text;

namespace HotChocolate.Fusion.Execution.Nodes.Serialization;

internal sealed class CodeWriter(StringBuilder sb)
{
    private int _indent;

    public void Indent() => _indent++;

    public void Unindent() => _indent--;

    public void WriteLine(string line)
    {
        for (var i = 0; i < _indent; i++)
        {
            sb.Append("  ");
        }

        sb.AppendLine(line);
    }

    public void WriteLine(string format, params object[] args)
    {
        for (var i = 0; i < _indent; i++)
        {
            sb.Append("  ");
        }

        sb.AppendFormat(format, args).AppendLine();
    }

    public void Write(string s)
    {
        sb.Append(s);
    }
}
