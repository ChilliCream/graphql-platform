using System;
using System.Collections.Generic;
using HotChocolate.Execution.Properties;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing;

internal sealed class SelectionVariants : ISelectionVariants
{
    private IObjectType? _firstType;
    private ISelectionSet? _firstSelections;
    private IObjectType? _secondType;
    private ISelectionSet? _secondSelections;
    private Dictionary<IObjectType, ISelectionSet>? _map;
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
            return _map.TryGetValue(typeContext, out ISelectionSet? selections)
                ? selections
                : SelectionSet.Empty;
        }

        if (ReferenceEquals(_firstType, typeContext))
        {
            return _firstSelections!;
        }

        if (ReferenceEquals(_secondType, typeContext))
        {
            return _secondSelections!;
        }

        return SelectionSet.Empty;
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
        IObjectType typeContext,
        IReadOnlyList<ISelection> selections,
        IReadOnlyList<IFragment>? fragments,
        bool isConditional)
    {
        if (_readOnly)
        {
            throw new NotSupportedException(Resources.SelectionVariants_ReadOnly);
        }

        var selectionSet = new SelectionSet(selections, fragments, isConditional);

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
                    throw new InvalidOperationException(
                        $"The type {typeContext.Name} was already added.");
                }

                _secondType = typeContext;
                _secondSelections = selectionSet;
            }
            else
            {
                _map = new Dictionary<IObjectType, ISelectionSet>
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

    public void Seal()
    {
        _readOnly = true;
    }
}
