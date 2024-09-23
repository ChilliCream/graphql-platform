using HotChocolate.Types;
using static HotChocolate.Execution.Properties.Resources;
using static HotChocolate.Execution.ThrowHelper;

namespace HotChocolate.Execution.Processing;

internal sealed class SelectionVariants(int id) : ISelectionVariants
{
    private IObjectType? _firstType;
    private SelectionSet? _firstSelectionSet;
    private IObjectType? _secondType;
    private SelectionSet? _secondSelectionSet;
    private Dictionary<IObjectType, SelectionSet>? _map;
    private bool _readOnly;

    /// <inheritdoc />
    public int Id { get; } = id;

    /// <inheritdoc />
    public IOperation DeclaringOperation { get; private set; } = default!;

    public IEnumerable<IObjectType> GetPossibleTypes()
        => _map?.Keys ?? GetPossibleTypesLazy();

    public bool IsPossibleType(IObjectType typeContext)
    {
        if(_map is not null)
        {
            return _map.ContainsKey(typeContext);
        }

        if (ReferenceEquals(_firstType, typeContext))
        {
            return true;
        }

        if (ReferenceEquals(_secondType, typeContext))
        {
            return true;
        }

        return false;
    }

    private IEnumerable<IObjectType> GetPossibleTypesLazy()
    {
        yield return _firstType!;

        if (_secondType is not null)
        {
            yield return _secondType;
        }
    }

    public ISelectionSet GetSelectionSet(IObjectType typeContext)
    {
        if (_map is not null)
        {
            if (_map.TryGetValue(typeContext, out var selections))
            {
                return selections;
            }
            else
            {
                throw SelectionSet_TypeContextInvalid(typeContext);
            }
        }

        if (ReferenceEquals(_firstType, typeContext))
        {
            return _firstSelectionSet!;
        }

        if (ReferenceEquals(_secondType, typeContext))
        {
            return _secondSelectionSet!;
        }

        throw SelectionSet_TypeContextInvalid(typeContext);
    }

    internal bool ContainsSelectionSet(IObjectType typeContext)
    {
        if (_map is not null)
        {
            return _map.ContainsKey(typeContext);
        }

        if (ReferenceEquals(_firstType, typeContext))
        {
            return true;
        }

        if (ReferenceEquals(_secondType, typeContext))
        {
            return true;
        }

        return false;
    }

    internal void AddSelectionSet(
        int id,
        ObjectType typeContext,
        Selection[] selections,
        Fragment[]? fragments,
        bool isConditional)
    {
        if (_readOnly)
        {
            throw new NotSupportedException(SelectionVariants_ReadOnly);
        }

        var selectionSet = new SelectionSet(id, selections, fragments, isConditional);

        if (_map is not null)
        {
            _map[typeContext] = selectionSet;
        }
        else
        {
            if (_firstType is null)
            {
                _firstType = typeContext;
                _firstSelectionSet = selectionSet;
            }
            else if (_secondType is null)
            {
                if (typeContext == _firstType)
                {
                    throw SelectionSet_TypeAlreadyAdded(typeContext);
                }

                _secondType = typeContext;
                _secondSelectionSet = selectionSet;
            }
            else
            {
                _map = new Dictionary<IObjectType, SelectionSet>
                {
                    { _firstType, _firstSelectionSet! },
                    { _secondType, _secondSelectionSet! },
                    { typeContext, selectionSet },
                };

                _firstType = null;
                _firstSelectionSet = null;
                _secondType = null;
                _secondSelectionSet = null;
            }
        }
    }

    /// <summary>
    /// Completes the selection variant without sealing it.
    /// </summary>
    internal void Complete(IOperation declaringOperation)
    {
        if (!_readOnly)
        {
            DeclaringOperation = declaringOperation;
            _firstSelectionSet?.Complete(declaringOperation);
            _secondSelectionSet?.Complete(declaringOperation);

            if (_map is not null)
            {
                foreach (var selectionSet in _map.Values)
                {
                    selectionSet.Complete(declaringOperation);
                }
            }
        }
    }

    internal void Seal(IOperation declaringOperation)
    {
        if (!_readOnly)
        {
            DeclaringOperation = declaringOperation;
            _firstSelectionSet?.Seal(declaringOperation);
            _secondSelectionSet?.Seal(declaringOperation);

            if (_map is not null)
            {
                foreach (var selectionSet in _map.Values)
                {
                    selectionSet.Seal(declaringOperation);
                }
            }

            _readOnly = true;
        }
    }
}
