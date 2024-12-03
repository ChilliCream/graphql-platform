using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Utilities;
using static HotChocolate.Execution.Properties.Resources;

namespace HotChocolate.Execution.Processing;

/// <summary>
/// The <see cref="SelectionSet"/> optimizer provides helper methods
/// to optimize a <see cref="SelectionSet"/>.
/// </summary>
public readonly ref struct SelectionSetOptimizerContext
{
    private readonly OperationCompiler _compiler;
    private readonly OperationCompiler.CompilerContext _compilerContext;
    private readonly Dictionary<Selection, OperationCompiler.SelectionSetInfo[]> _selectionLookup;
    private readonly CreateFieldPipeline _createFieldPipeline;

    /// <summary>
    /// Initializes a new instance of <see cref="SelectionSetOptimizerContext"/>
    /// </summary>
    internal SelectionSetOptimizerContext(
        OperationCompiler compiler,
        OperationCompiler.CompilerContext compilerContext,
        Dictionary<Selection, OperationCompiler.SelectionSetInfo[]> selectionLookup,
        Dictionary<string, object?> contextData,
        CreateFieldPipeline createFieldPipeline,
        SelectionPath path)
    {
        _compiler = compiler;
        _compilerContext = compilerContext;
        _selectionLookup = selectionLookup;
        _createFieldPipeline = createFieldPipeline;
        ContextData = contextData;
        Path = path;
    }

    /// <summary>
    /// Gets the schema for which the query is compiled.
    /// </summary>
    public ISchema Schema
        => _compilerContext.Schema;

    /// <summary>
    /// Gets the type context of the current selection-set.
    /// </summary>
    public IObjectType Type
        => _compilerContext.Type;

    /// <summary>
    /// Gets the selections of this selection set.
    /// </summary>
    public IReadOnlyDictionary<string, Selection> Selections
        => _compilerContext.Fields;

    /// <summary>
    /// The context data dictionary can be used by middleware components and
    /// resolvers to store and retrieve data during execution.
    /// </summary>
    public IDictionary<string, object?> ContextData { get; }

    /// <summary>
    /// Gets the current selection path.
    /// </summary>
    public SelectionPath Path { get; }

    /// <summary>
    /// Gets the next operation unique selection id.
    /// </summary>
    public int GetNextSelectionId()
        => _compiler.GetNextSelectionId();

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
    public FieldDelegate CompileResolverPipeline(IObjectField field, FieldNode selection)
        => _createFieldPipeline(Schema, field, selection);

    /// <summary>
    /// Adds an additional selection for internal purposes.
    /// </summary>
    /// <param name="newSelection">
    /// The new optimized selection.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="newSelection"/> is <c>null</c>.
    /// </exception>
    public void AddSelection(Selection newSelection)
    {
        if (newSelection is null)
        {
            throw new ArgumentNullException(nameof(newSelection));
        }

        _compilerContext.Fields.Add(newSelection.ResponseName, newSelection);
        _compiler.RegisterNewSelection(newSelection);
    }

    /// <summary>
    /// Adds an additional selection for internal purposes.
    /// </summary>
    /// <param name="responseName">
    /// The selection response name.
    /// </param>
    /// <param name="newSelection">
    /// The new optimized selection.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="newSelection"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The <paramref name="responseName"/> is not a valid GraphQL field name.
    /// </exception>
    [Obsolete("Use AddSelection(Selection) instead.")]
    public void AddSelection(string responseName, Selection newSelection)
    {
        if (newSelection is null)
        {
            throw new ArgumentNullException(nameof(newSelection));
        }

        responseName.EnsureGraphQLName();

        if (!newSelection.ResponseName.EqualsOrdinal(responseName))
        {
            throw new ArgumentException(
                SelectionSetOptimizerContext_AddSelection_ResponseNameNotTheSame);
        }

        _compilerContext.Fields.Add(responseName, newSelection);
        _compiler.RegisterNewSelection(newSelection);
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
        if (newSelection is null)
        {
            throw new ArgumentNullException(nameof(newSelection));
        }

        if (!_compilerContext.Fields.TryGetValue(
            newSelection.ResponseName,
            out var currentSelection))
        {
            throw new ArgumentException($"The `{newSelection.ResponseName}` does not exist.");
        }

        _compilerContext.Fields[newSelection.ResponseName] = newSelection;

        if (_selectionLookup.TryGetValue(currentSelection, out var selectionSetInfos))
        {
            _selectionLookup.Remove(currentSelection);
            _selectionLookup.Add(newSelection, selectionSetInfos);
        }
    }

    /// <summary>
    /// Replaces an existing selection with an optimized version.
    /// </summary>
    /// <param name="responseName">
    /// The selection response name.
    /// </param>
    /// <param name="newSelection">
    /// The new optimized selection.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="newSelection"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// - The <paramref name="responseName"/> is not a valid GraphQL field name.
    /// - There is no existing selection with the specified <paramref name="responseName"/>.
    /// - <see cref="Selection.ResponseName"/> is not equal to <paramref name="responseName"/>.
    /// </exception>
    [Obsolete("Use ReplaceSelection(Selection) instead.")]
    public void ReplaceSelection(string responseName, Selection newSelection)
    {
        if (!responseName.IsValidGraphQLName())
        {
            throw new ArgumentException(
                string.Format(SelectionSetOptimizerContext_InvalidFieldName, responseName));
        }

        if (newSelection is null)
        {
            throw new ArgumentNullException(nameof(newSelection));
        }

        if (!_compilerContext.Fields.TryGetValue(responseName, out var currentSelection))
        {
            throw new ArgumentException($"The `{responseName}` does not exist.");
        }

        _compilerContext.Fields[responseName] = newSelection;

        if (_selectionLookup.TryGetValue(currentSelection, out var selectionSetInfos))
        {
            _selectionLookup.Remove(currentSelection);
            _selectionLookup.Add(newSelection, selectionSetInfos);
        }
    }
}
