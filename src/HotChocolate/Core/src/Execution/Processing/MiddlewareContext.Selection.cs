using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing;

internal partial class MiddlewareContext
{
    private readonly PureResolverContext _childContext;
    private ISelection _selection = null!;

    public ObjectType ObjectType => _selection.DeclaringType;

    public ObjectField Field => _selection.Field;

    public ISelection Selection => _selection;

    public string ResponseName => _selection.ResponseName;

    public int ResponseIndex { get; private set; }

    public FieldDelegate? ResolverPipeline => _selection.ResolverPipeline;

    public PureFieldDelegate? PureResolver => _selection.PureResolver;

    public bool TryCreatePureContext(
        ISelection selection,
        ObjectType parentType,
        ObjectResult parentResult,
        object? parent,
        [NotNullWhen(true)] out IResolverContext? context)
    {
        if (_childContext.Initialize(selection, parentType, parentResult, parent))
        {
            context = _childContext;
            return true;
        }

        context = null;
        return false;
    }

    public IReadOnlyList<ISelection> GetSelections(
        ObjectType typeContext,
        ISelection? selection = null,
        bool allowInternals = false)
    {
        ArgumentNullException.ThrowIfNull(typeContext);

        selection ??= _selection;

        if (selection.SelectionSet is null)
        {
            return [];
        }

        var selectionSet = _operationContext.CollectFields(selection, typeContext);

        if (selectionSet.IsConditional)
        {
            var operationIncludeFlags = _operationContext.IncludeFlags;
            var selectionCount = selectionSet.Selections.Count;
            ref var selectionRef = ref ((SelectionSet)selectionSet).GetSelectionsReference();
            var finalFields = new List<ISelection>();

            for (var i = 0; i < selectionCount; i++)
            {
                var childSelection = Unsafe.Add(ref selectionRef, i);

                if (childSelection.IsIncluded(operationIncludeFlags, allowInternals))
                {
                    finalFields.Add(childSelection);
                }
            }

            return finalFields;
        }

        return selectionSet.Selections;
    }

    public ISelectionCollection Select()
    {
        var schema = Unsafe.As<Schema>(_operationContext.Schema);
        return new SelectionCollection(schema, Operation, [Selection], _operationContext.IncludeFlags);
    }

    public ISelectionCollection Select(string fieldName)
        => Select().Select(fieldName);
}
