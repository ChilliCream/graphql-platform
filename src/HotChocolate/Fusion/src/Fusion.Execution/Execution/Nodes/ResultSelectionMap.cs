using System.Runtime.CompilerServices;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Nodes;

/// <summary>
/// A pre-computed, allocation-free lookup structure that mirrors a <see cref="SelectionSetNode"/>
/// for tracking which fields in the result tree belong to an execution node.
/// Built once at node construction time; queried per-field during value completion.
/// </summary>
internal sealed class ResultSelectionMap
{
    private readonly string[] _responseNames;
    private readonly Dictionary<string, ResultSelectionMap?> _children;

    private ResultSelectionMap(
        string[] responseNames,
        Dictionary<string, ResultSelectionMap?> children,
        SelectionSetNode selectionSet)
    {
        _responseNames = responseNames;
        _children = children;
        SelectionSet = selectionSet;
    }

    /// <summary>
    /// The pre-computed response names at this level.
    /// </summary>
    public ReadOnlySpan<string> ResponseNames => _responseNames;

    /// <summary>
    /// The original AST node, retained for serialization.
    /// </summary>
    public SelectionSetNode SelectionSet { get; }

    /// <summary>
    /// Gets the child map for a given response name, or <c>null</c> if the field is a leaf.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ResultSelectionMap? TryGetChild(string responseName)
        => _children.GetValueOrDefault(responseName);

    /// <summary>
    /// Creates a <see cref="ResultSelectionMap"/> from a <see cref="SelectionSetNode"/>.
    /// </summary>
    public static ResultSelectionMap Create(SelectionSetNode selectionSet)
    {
        var responseNames = new List<string>();
        var children = new Dictionary<string, ResultSelectionMap?>(StringComparer.Ordinal);

        CollectFields(selectionSet, responseNames, children);

        return new ResultSelectionMap(
            responseNames.ToArray(),
            children,
            selectionSet);
    }

    private static void CollectFields(
        SelectionSetNode selectionSet,
        List<string> responseNames,
        Dictionary<string, ResultSelectionMap?> children)
    {
        foreach (var selection in selectionSet.Selections)
        {
            switch (selection)
            {
                case FieldNode field:
                    var name = field.Alias?.Value ?? field.Name.Value;
                    responseNames.Add(name);
                    children[name] = field.SelectionSet is { } childSet
                        ? Create(childSet)
                        : null;
                    break;

                case InlineFragmentNode inlineFragment:
                    CollectFields(inlineFragment.SelectionSet, responseNames, children);
                    break;
            }
        }
    }
}
