using System.Text;
using HotChocolate.Execution.Processing;

namespace HotChocolate.Fusion.Planning;

internal sealed class SelectionPath : IEquatable<SelectionPath>
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

    public bool Equals(SelectionPath? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if(Parent is null)
        {
            if(other.Parent is not null)
            {
                return false;
            }

            return other.Selection.ResponseName.Equals(Selection.ResponseName);
        }

        if (other.Parent is null)
        {
            return false;
        }

        return Parent.Equals(other.Parent)
            && other.Selection.ResponseName.Equals(Selection.ResponseName);
    }

    public override bool Equals(object? obj)
        => obj is SelectionPath other && Equals(other);

    public override int GetHashCode()
        => HashCode.Combine(Selection.ResponseName, Parent?.GetHashCode());

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
