using System.Text;
using HotChocolate.Execution.Processing;

namespace HotChocolate.Fusion.Planning;

internal sealed class SelectionPath
{
    public SelectionPath(ISelection selection)
        : this(selection, null)
    {
    }

    private SelectionPath(ISelection selection, SelectionPath? parent)
    {
        Selection = selection;
        Parent = parent;
    }

    public SelectionPath? Parent { get; }

    public ISelection Selection { get; }

    public SelectionPath Append(ISelection selection)
        => new(selection, this);

    public override string ToString()
    {
        if (Parent is null)
        {
            return $"/{Selection.ResponseName}";
        }

        var sb = new StringBuilder();
        var current = this;

        while (current is not null)
        {
            sb.Insert(0, '/');
            sb.Insert(1, current.Selection.ResponseName);
            current = current.Parent;
        }

        return sb.ToString();
    }
}
