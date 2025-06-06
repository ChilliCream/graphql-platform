using System.Text;

namespace HotChocolate.Fusion.Planning;

internal sealed class CodeWriter(StringBuilder sb)
{
    private int indent = 0;

    public void Indent() => indent++;

    public void Unindent() => indent--;

    public void WriteLine(string line)
    {
        for (var i = 0; i < indent; i++)
        {
            sb.Append("  ");
        }

        sb.AppendLine(line);
    }

    public void Write(string s)
    {
        sb.Append(s);
    }
}
