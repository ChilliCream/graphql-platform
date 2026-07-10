using System.Runtime.CompilerServices;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution.Nodes;

/// <summary>
/// A pre-computed lookup structure that mirrors a <see cref="SelectionSetNode"/>
/// for tracking which fields in the result tree belong to an execution node.
/// </summary>
internal abstract class ResultSelectionSet(
    ResultFragment[] fragments,
    string[] allResponseNames)
{
    private const int SmallThreshold = 8;

    /// <summary>
    /// The pre-computed union of ALL response names at this level,
    /// including those inside inline fragments. Used by error pocketing
    /// and error result building where over-approximation is safe.
    /// </summary>
    public ReadOnlySpan<string> ResponseNames => allResponseNames;

    /// <summary>
    /// Gets the direct selections at this level.
    /// </summary>
    protected abstract ReadOnlySpan<ResultSelection> DirectSelections { get; }

    /// <summary>
    /// Gets the child selection set for a given response name (type-unaware).
    /// Searches direct selections first, then fragments (first match wins).
    /// Used at the <c>BuildResult</c> level where the runtime type isn't resolved yet.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ResultSelectionSet? TryGetChild(string responseName)
        => TryGetDirectChild(responseName, out var selectionSet)
            ? selectionSet
            : TryGetFragmentChild(responseName);

    /// <summary>
    /// Gets the child selection set for a given response name, filtered by type condition.
    /// Searches direct selections first, then only fragments whose type condition
    /// is <c>null</c> or is assignable from <paramref name="objectType"/>.
    /// Used in <c>TryCompleteObjectValue</c> where the runtime type is known.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ResultSelectionSet? TryGetChild(string responseName, IObjectTypeDefinition objectType)
        => TryGetDirectChild(responseName, out var selectionSet)
            ? selectionSet
            : TryGetFragmentChild(responseName, objectType);

    /// <summary>
    /// Tries to find a child in direct selections. Implemented by subclasses
    /// (linear scan for small sets, dictionary for large sets).
    /// </summary>
    /// <returns><c>true</c> if the response name was found in direct selections.</returns>
    protected abstract bool TryGetDirectChild(string responseName, out ResultSelectionSet? child);

    private ResultSelectionSet? TryGetFragmentChild(string responseName)
    {
        for (var i = 0; i < fragments.Length; i++)
        {
            ref readonly var fragment = ref fragments[i];

            if (fragment.Body.TryGetChild(responseName) is { } result)
            {
                return result;
            }
        }

        return null;
    }

    private ResultSelectionSet? TryGetFragmentChild(string responseName, IObjectTypeDefinition objectType)
    {
        for (var i = 0; i < fragments.Length; i++)
        {
            ref readonly var fragment = ref fragments[i];

            if (fragment.TypeCondition?.IsAssignableFrom(objectType) == false)
            {
                continue;
            }

            var result = fragment.Body.TryGetChild(responseName, objectType);
            if (result is not null)
            {
                return result;
            }
        }

        return null;
    }

    /// <summary>
    /// Reconstructs a <see cref="SelectionSetNode"/> from this selection set.
    /// </summary>
    public SelectionSetNode ToSelectionSetNode()
    {
        var selections = new List<ISelectionNode>();

        foreach (var selection in DirectSelections)
        {
            selections.Add(new FieldNode(
                selection.ResponseName,
                selectionSet: selection.Child?.ToSelectionSetNode()));
        }

        foreach (var fragment in fragments)
        {
            selections.Add(new InlineFragmentNode(
                null,
                fragment.TypeCondition is not null
                    ? new NamedTypeNode(fragment.TypeCondition.Name)
                    : null,
                [],
                fragment.Body.ToSelectionSetNode()));
        }

        return new SelectionSetNode(selections);
    }

    /// <summary>
    /// Returns the GraphQL syntax representation of this selection set.
    /// </summary>
    public string ToString(bool indented)
        => ToSelectionSetNode().ToString(indented);

    /// <inheritdoc />
    public override string ToString()
        => ToString(indented: false);

    /// <summary>
    /// Creates a <see cref="ResultSelectionSet"/> from a <see cref="SelectionSetNode"/>.
    /// </summary>
    /// <param name="selectionSet">The AST selection set to build from.</param>
    /// <param name="schema">
    /// Optional schema used to resolve inline fragment type conditions to
    /// <see cref="ITypeDefinition"/> instances. When <c>null</c>, type conditions are not resolved.
    /// </param>
    public static ResultSelectionSet Create(SelectionSetNode selectionSet, ISchemaDefinition? schema = null)
    {
        var directSelections = new List<ResultSelection>();
        var fragments = new List<ResultFragment>();
        var allResponseNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (var selection in selectionSet.Selections)
        {
            switch (selection)
            {
                case FieldNode field:
                    var name = field.Alias?.Value ?? field.Name.Value;
                    allResponseNames.Add(name);
                    directSelections.Add(new ResultSelection(
                        name,
                        field.SelectionSet is { } childSet ? Create(childSet, schema) : null));
                    break;

                case InlineFragmentNode inlineFragment:
                    var body = Create(inlineFragment.SelectionSet, schema);

                    ITypeDefinition? typeCondition = null;
                    if (inlineFragment.TypeCondition is not null)
                    {
                        schema?.Types.TryGetType(
                            inlineFragment.TypeCondition.Name.Value,
                            out typeCondition);
                    }

                    fragments.Add(new ResultFragment(typeCondition, body));

                    // Add the fragment body's response names to the union.
                    foreach (var responseName in body.ResponseNames)
                    {
                        allResponseNames.Add(responseName);
                    }

                    break;
            }
        }

        var selectionsArray = directSelections.ToArray();
        var fragmentsArray = fragments.ToArray();
        var responseNamesArray = new string[allResponseNames.Count];
        allResponseNames.CopyTo(responseNamesArray);

        if (selectionsArray.Length < SmallThreshold)
        {
            return new SmallResultSelectionSet(
                selectionsArray,
                fragmentsArray,
                responseNamesArray);
        }

        return new LargeResultSelectionSet(
            selectionsArray,
            fragmentsArray,
            responseNamesArray);
    }
}
