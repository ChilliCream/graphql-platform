using System.Collections;
using System.Collections.Immutable;
using HotChocolate.Language;
using HotChocolate.Types;
using static HotChocolate.Execution.Properties.Resources;
using static HotChocolate.Execution.ThrowHelper;

namespace HotChocolate.Execution.Processing;

internal sealed class Operation : IOperation
{
    private readonly object _writeLock = new();
    private SelectionVariants[] _selectionVariants = [];
    private IncludeCondition[] _includeConditions = [];
    private ImmutableDictionary<string, object?> _contextData = ImmutableDictionary<string, object?>.Empty;
    private bool _sealed;

    public Operation(
        string id,
        DocumentNode document,
        OperationDefinitionNode definition,
        ObjectType rootType,
        ISchema schema)
    {
        Id = id;
        Document = document;
        Definition = definition;
        RootType = rootType;
        Type = definition.Operation;
        Schema = schema;

        if (definition.Name?.Value is { } name)
        {
            Name = name;
        }
    }

    public string Id { get; }

    public DocumentNode Document { get; }

    public OperationDefinitionNode Definition { get; }

    public ObjectType RootType { get; }

    public string? Name { get; }

    public OperationType Type { get; }

    public ISelectionSet RootSelectionSet { get; private set; } = default!;

    public IReadOnlyList<ISelectionVariants> SelectionVariants
        => _selectionVariants;

    public bool HasIncrementalParts { get; private set; }

    public IReadOnlyList<IncludeCondition> IncludeConditions
        => _includeConditions;

    public IReadOnlyDictionary<string, object?> ContextData => _contextData;

    public ISchema Schema { get; }

    public ISelectionSet GetSelectionSet(ISelection selection, IObjectType typeContext)
    {
        if (selection is null)
        {
            throw new ArgumentNullException(nameof(selection));
        }

        if (typeContext is null)
        {
            throw new ArgumentNullException(nameof(typeContext));
        }

        var selectionSetId = ((Selection)selection).SelectionSetId;

        if (selectionSetId is -1)
        {
            throw Operation_NoSelectionSet();
        }

        return _selectionVariants[selectionSetId].GetSelectionSet(typeContext);
    }

    public IEnumerable<IObjectType> GetPossibleTypes(ISelection selection)
    {
        if (selection is null)
        {
            throw new ArgumentNullException(nameof(selection));
        }

        var selectionSetId = ((Selection)selection).SelectionSetId;

        if (selectionSetId == -1)
        {
            throw new ArgumentException(Operation_GetPossibleTypes_NoSelectionSet);
        }

        return _selectionVariants[selectionSetId].GetPossibleTypes();
    }

    public long CreateIncludeFlags(IVariableValueCollection variables)
    {
        long context = 0;

        for (var i = 0; i < _includeConditions.Length; i++)
        {
            if (_includeConditions[i].IsIncluded(variables))
            {
                long flag = 1;
                flag <<= i;
                context |= flag;
            }
        }

        return context;
    }

    public bool TryGetState<TState>(out TState? state)
    {
        var key = typeof(TState).FullName ?? throw new InvalidOperationException();
        return TryGetState(key, out state);
    }

    public bool TryGetState<TState>(string key, out TState? state)
    {
        if(_contextData.TryGetValue(key, out var value)
            && value is TState casted)
        {
            state = casted;
            return true;
        }

        state = default;
        return false;
    }

    public TState GetOrAddState<TState>(Func<TState> createState)
        => GetOrAddState<TState, object?>(_ => createState(), null);

    public TState GetOrAddState<TState, TContext>(Func<TContext, TState> createState, TContext context)
    {
        var key = typeof(TState).FullName ?? throw new InvalidOperationException();

        // ReSharper disable once InconsistentlySynchronizedField
        if(!_contextData.TryGetValue(key, out var state))
        {
            lock (_writeLock)
            {
                if(!_contextData.TryGetValue(key, out state))
                {
                    var newState = createState(context);
                    _contextData = _contextData.SetItem(key, newState);
                    return newState;
                }
            }
        }

        return (TState)state!;
    }

    public TState GetOrAddState<TState, TContext>(
        string key,
        Func<string, TState> createState)
        => GetOrAddState<TState, object?>(key, (k, _) => createState(k), null);

    public TState GetOrAddState<TState, TContext>(
        string key,
        Func<string, TContext, TState> createState,
        TContext context)
    {
        // ReSharper disable once InconsistentlySynchronizedField
        if(!_contextData.TryGetValue(key, out var state))
        {
            lock (_writeLock)
            {
                if(!_contextData.TryGetValue(key, out state))
                {
                    var newState = createState(key, context);
                    _contextData = _contextData.SetItem(key, newState);
                    return newState;
                }
            }
        }

        return (TState)state!;
    }

    internal void Seal(
        IReadOnlyDictionary<string, object?> contextData,
        SelectionVariants[] selectionVariants,
        bool hasIncrementalParts,
        IncludeCondition[] includeConditions)
    {
        if (!_sealed)
        {
            _contextData = contextData.ToImmutableDictionary();
            var root = selectionVariants[0];
            RootSelectionSet = root.GetSelectionSet(RootType);
            _selectionVariants = selectionVariants;
            HasIncrementalParts = hasIncrementalParts;
            _includeConditions = includeConditions;
            _sealed = true;
        }
    }

    public IEnumerator<ISelectionSet> GetEnumerator()
    {
        foreach (var selectionVariant in _selectionVariants)
        {
            foreach (var objectType in selectionVariant.GetPossibleTypes())
            {
                yield return selectionVariant.GetSelectionSet(objectType);
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public override string ToString() => OperationPrinter.Print(this);
}
