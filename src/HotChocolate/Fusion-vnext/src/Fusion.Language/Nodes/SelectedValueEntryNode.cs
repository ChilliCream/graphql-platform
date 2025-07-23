using System.Linq.Expressions;

namespace HotChocolate.Fusion.Language;



public interface INonConditionalValueSelectionNode : IValueSelectionNode;

public interface IValueSelectionNode : IFieldSelectionMapSyntaxNode;









public sealed class PathAndSelectedObjectValueNode : IValueSelectionNode
{
    public PathAndSelectedObjectValueNode(PathNode path, SelectedObjectValueNode selectedObjectValue)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(selectedObjectValue);

        Path = path;
        SelectedObjectValue = selectedObjectValue;
    }

    public PathNode Path { get; }

    public SelectedObjectValueNode SelectedObjectValue { get; }
}

public sealed class PathAndSelectedListValueNode : IValueSelectionNode
{
    public PathAndSelectedListValueNode(PathNode path, ListValueSelectionNode listValueSelection)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(listValueSelection);

        Path = path;
        ListValueSelection = listValueSelection;
    }

    public PathNode Path { get; }

    public ListValueSelectionNode ListValueSelection { get; }
}


