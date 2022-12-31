using System;
using System.Collections.Generic;
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
        CreateFieldPipeline createFieldPipeline)
    {
        _compiler = compiler;
        _compilerContext = compilerContext;
        _selectionLookup = selectionLookup;
        _createFieldPipeline = createFieldPipeline;
        ContextData = contextData;
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
    /// <param name="responseName">
    /// The selection response name.
    /// </param>
    /// <param name="newSelection">
    /// The new optimized selection.
    /// </param>
    /// <exception cref="ArgumentException">
    /// The <paramref name="responseName"/> is not a valid GraphQL field name.
    /// </exception>
    public void AddSelection(string responseName, Selection newSelection)
    {
        responseName.EnsureGraphQLName();
        _compilerContext.Fields.Add(responseName, newSelection);
        _compiler.RegisterNewSelection(newSelection);
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
    /// <exception cref="ArgumentException">
    /// - The <paramref name="responseName"/> is not a valid GraphQL field name.
    /// - There is no existing selection with the specified <paramref name="responseName"/>.
    /// </exception>
    public void ReplaceSelection(string responseName, Selection newSelection)
    {
        if (!responseName.IsValidGraphQLName())
        {
            throw new ArgumentException(
                string.Format(SelectionSetOptimizerContext_InvalidFieldName, responseName));
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
