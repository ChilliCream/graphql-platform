using System;
using System.Collections.Generic;
using HotChocolate.Types;
using static HotChocolate.Execution.Properties.Resources;
using static HotChocolate.Execution.ThrowHelper;

namespace HotChocolate.Execution.Processing;

internal sealed class SelectionVariants : ISelectionVariants
{
    private IObjectType? _firstType;
    private SelectionSet? _firstSelections;
    private IObjectType? _secondType;
    private SelectionSet? _secondSelections;
    private Dictionary<IObjectType, SelectionSet>? _map;
    private bool _readOnly;

    public SelectionVariants(int id)
    {
        Id = id;
    }

    public int Id { get; }

    public IEnumerable<IObjectType> GetPossibleTypes()
        => _map?.Keys ?? GetPossibleTypesLazy();

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
            return _map.TryGetValue(typeContext, out var selections)
                ? selections
                : throw SelectionSet_TypeContextInvalid(typeContext);
        }

        if (ReferenceEquals(_firstType, typeContext))
        {
            return _firstSelections!;
        }

        if (ReferenceEquals(_secondType, typeContext))
        {
            return _secondSelections!;
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
                _firstSelections = selectionSet;
            }
            else if (_secondType is null)
            {
                if (typeContext == _firstType)
                {
                    throw SelectionSet_TypeAlreadyAdded(typeContext);
                }

                _secondType = typeContext;
                _secondSelections = selectionSet;
            }
            else
            {
                _map = new Dictionary<IObjectType, SelectionSet>
                {
                    { _firstType, _firstSelections! },
                    { _secondType, _secondSelections! },
                    { typeContext, selectionSet }
                };

                _firstType = null;
                _firstSelections = null;
                _secondType = null;
                _secondSelections = null;
            }
        }
    }

    internal void Seal()
    {
        if (!_readOnly)
        {
            _firstSelections?.Seal();
            _secondSelections?.Seal();

            if (_map is not null)
            {
                foreach (var selectionSet in _map.Values)
                {
                    selectionSet.Seal();
                }
            }

            _readOnly = true;
        }
    }
}
