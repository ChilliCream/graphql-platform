using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Execution.Processing;

/// <summary>
/// The <see cref="SelectionSet"/> optimizer provides helper methods
/// to optimize a <see cref="SelectionSet"/>.
/// </summary>
public ref struct SelectionSetOptimizerContext
{
    private readonly ref ImmutableArray<Selection> _selections;
    private readonly OperationFeatureCollection _features;
    private readonly ref int _lastSelectionId;
    private readonly Func<Schema, ObjectField, FieldNode, FieldDelegate> _createFieldPipeline;
    private Dictionary<string, Selection>? _selectionMap;
    /// <summary>
    /// Initializes a new instance of <see cref="SelectionSetOptimizerContext"/>
    /// </summary>
    internal SelectionSetOptimizerContext(
        SelectionPath path,
        ObjectType typeContext,
        ref ImmutableArray<Selection> selections,
        OperationFeatureCollection features,
        ref int lastSelectionId,
        Schema schema,
        Func<Schema, ObjectField, FieldNode, FieldDelegate> createFieldPipeline)
    {
        _selections = ref selections;
        _features = features;
        _lastSelectionId = ref lastSelectionId;
        _createFieldPipeline = createFieldPipeline;
        Path = path;
        TypeContext = typeContext;
        Schema = schema;
    }

    /// <summary>
    /// Gets the schema for which the query is compiled.
    /// </summary>
    public Schema Schema { get; }

    /// <summary>
    /// Gets the path where this selection set is located within the GraphQL operation document.
    /// </summary>
    public SelectionPath Path { get; }

    /// <summary>
    /// Gets the type context of the current selection-set.
    /// </summary>
    public ObjectType TypeContext { get; }

    public ImmutableArray<Selection> Selections => _selections;

    public bool ContainsField(string fieldName)
        => _selections.Any(t => t.Field.Name.EqualsOrdinal(fieldName));

    public bool ContainsResponseName(string responseName)
    {
        _selectionMap ??= _selections.ToDictionary(t => t.ResponseName);
        return _selectionMap.ContainsKey(responseName);
    }

    public bool TryGetSelection(string responseName, [MaybeNullWhen(false)] out Selection value)
    {
        _selectionMap ??= _selections.ToDictionary(t => t.ResponseName);
        return _selectionMap.TryGetValue(responseName, out value);
    }

    public Selection GetSelection(string responseName)
    {
        _selectionMap ??= _selections.ToDictionary(t => t.ResponseName);
        return _selectionMap[responseName];
    }

    public SelectionFeatureCollection CreateSelectionFeatures(Selection selection)
        => new SelectionFeatureCollection(_features, selection.Id);

    /// <summary>
    /// Gets the next operation unique selection id.
    /// </summary>
    public int NewSelectionId()
        => ++_lastSelectionId;

    /// <summary>
    /// Sets the resolvers on the specified <paramref name="selection"/>.
    /// </summary>
    /// <param name="selection">
    /// The selection to set the resolvers on.
    /// </param>
    /// <param name="resolverPipeline">
    /// The async resolver pipeline.
    /// </param>
    /// <param name="pureResolver">
    /// The pure resolver.
    /// </param>
    public void SetResolver(
        Selection selection,
        FieldDelegate? resolverPipeline = null,
        PureFieldDelegate? pureResolver = null)
        => selection.SetResolvers(resolverPipeline, pureResolver);

    /// <summary>
    /// Allows to compile the field resolver pipeline for a field.
    /// </summary>
    /// <param name="field">The field.</param>
    /// <param name="selection">The selection of the field.</param>
    /// <returns>
    /// Returns a <see cref="FieldDelegate" /> representing the field resolver pipeline.
    /// </returns>
    public FieldDelegate CompileResolverPipeline(ObjectField field, FieldNode selection)
        => _createFieldPipeline(Schema, field, selection);

    /// <summary>
    /// Adds a selection for internal purposes.
    /// </summary>
    /// <param name="internalSelection">
    /// The internal selection.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="internalSelection"/> is <c>null</c>.
    /// </exception>
    public void AddSelection(Selection internalSelection)
    {
        ArgumentNullException.ThrowIfNull(internalSelection);

        _selectionMap ??= _selections.ToDictionary(t => t.ResponseName);
        _selectionMap.Add(internalSelection.ResponseName, internalSelection);
        _selections = _selections.Add(internalSelection);
    }

    /// <summary>
    /// Replaces an existing selection with an optimized version.
    /// </summary>
    /// <param name="newSelection">
    /// The new optimized selection.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="newSelection"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// - There is no existing selection with the specified
    /// <paramref name="newSelection"/>.ResponseName.
    /// </exception>
    public void ReplaceSelection(Selection newSelection)
    {
        ArgumentNullException.ThrowIfNull(newSelection);

        _selectionMap ??= _selections.ToDictionary(t => t.ResponseName);

        if (!_selectionMap.TryGetValue(newSelection.ResponseName, out var currentSelection))
        {
            throw new ArgumentException($"The `{newSelection.ResponseName}` does not exist.");
        }

        _selectionMap[newSelection.ResponseName] = newSelection;
        var index = _selections.IndexOf(currentSelection);
        _selections = _selections.SetItem(index, newSelection);
    }
}
