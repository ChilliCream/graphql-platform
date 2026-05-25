using System.Diagnostics.CodeAnalysis;
using HotChocolate.Resolvers;
using HotChocolate.Text.Json;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing;

internal partial class MiddlewareContext
{
    private readonly PureResolverContext _childContext;
    private Selection _selection = null!;

    public ObjectType ObjectType => _selection.DeclaringType;

    public ObjectField Field => _selection.Field;

    public Selection Selection => _selection;

    public string ResponseName => _selection.ResponseName;

    public FieldDelegate? ResolverPipeline => _selection.ResolverPipeline;

    public PureFieldDelegate? PureResolver => _selection.PureResolver;

    public bool TryCreatePureContext(
        Selection selection,
        ObjectType selectionSetType,
        ResultElement resultValue,
        object? parent,
        [NotNullWhen(true)] out IResolverContext? context)
    {
        if (_childContext.Initialize(selection, selectionSetType, resultValue, parent))
        {
            context = _childContext;
            return true;
        }

        context = null;
        return false;
    }

    public SelectionEnumerator GetSelections(
        ObjectType typeContext,
        Selection? selection = null,
        bool allowInternals = false)
    {
        ArgumentNullException.ThrowIfNull(typeContext);

        selection ??= _selection;

        if (selection.IsLeaf)
        {
            return default;
        }

        var selectionSet = _operationContext.CollectFields(selection, typeContext);
        return new SelectionEnumerator(selectionSet, _operationContext.IncludeFlags);
    }

    public ISelectionCollection Select()
    {
        return new SelectionCollection(
            _operationContext.Schema,
            Operation,
            [Selection],
            _operationContext.IncludeFlags);
    }

    public ISelectionCollection Select(string fieldName)
        => Select().Select(fieldName);
}
