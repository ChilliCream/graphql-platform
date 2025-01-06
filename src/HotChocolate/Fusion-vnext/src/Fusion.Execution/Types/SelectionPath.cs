using System.Text;

namespace HotChocolate.Fusion.Types;

public sealed class SelectionPath
{
    private SelectionPath(
        SelectionPath? parent,
        string name,
        SelectionPathSegmentKind kind)
    {
        Parent = parent;
        Name = name;
        Kind = kind;
    }

    public SelectionPath? Parent { get; }

    public string Name { get; }

    public SelectionPathSegmentKind Kind { get; }

    public SelectionPath AppendField(string fieldName)
        => new(this, fieldName, SelectionPathSegmentKind.Field);

    public SelectionPath AppendFragment(string typeName)
        => new(this, typeName, SelectionPathSegmentKind.InlineFragment);

    public static SelectionPath Root { get; } =
        new(null, "root", SelectionPathSegmentKind.Root);

    public static SelectionPath Parse(string s)
    {
        var path = Root;
        var current = path;

        foreach (var segment in s.Split("."))
        {
            if (segment.Contains('<'))
            {
                var typeName = segment.Substring(segment.IndexOf('<') + 1, segment.Length - segment.IndexOf('>') - 2);
                current = current.AppendFragment(typeName);
            }
            else
            {
                current = current.AppendField(segment);
            }
        }

        return path;
    }

    public override string ToString()
    {
        var first = true;
        var path = new StringBuilder();
        var current = this;

        do
        {
            if (ReferenceEquals(current, Root))
            {
                break;
            }

            if (first)
            {
                first = false;
            }
            else
            {
                path.Insert(0, ".");
            }

            if (current.Kind == SelectionPathSegmentKind.InlineFragment)
            {
                path.Insert(0, $"<{current.Name}>");
            }
            else
            {
                path.Insert(0, current.Name);
            }

            current = current.Parent;
        } while (current != null);

        return path.ToString();
    }
}
