using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing;

internal sealed class SelectionVariants : ISelectionVariants
{
    private IObjectType? _firstType;
    private ISelectionSet? _firstSelections;
    private IObjectType? _secondType;
    private ISelectionSet? _secondSelections;
    private Dictionary<IObjectType, ISelectionSet>? _map;
    private IObjectType[]? _types;

    public SelectionVariants(SelectionSetNode selectionSet)
    {
        SelectionSet = selectionSet;
    }

    public SelectionSetNode SelectionSet { get; }

    public IReadOnlyList<IObjectType> GetPossibleTypes()
    {
        if (_types is not null)
        {
            return _types;
        }

        if (_map is { })
        {
            var types = new IObjectType[_map.Keys.Count];
            int index = 0; 
            foreach (IObjectType possibleType in _map.Keys)
            {
                types[index++] = possibleType;
            }
            _types = types;
        }
        else
        {
            int count = _secondType is not null ? 2 : 1;
            var types = new IObjectType[count];

            types[0] = _firstType!;

            if (_secondType is not null)
            {
                types[1] = _secondType;
            }

            _types = types;
        }

        return _types;
    }

    public ISelectionSet GetSelectionSet(IObjectType typeContext)
    {
        if (_map is { })
        {
            return _map.TryGetValue(typeContext, out ISelectionSet? selections)
                ? selections
                : Processing.SelectionSet.Empty;
        }

        if (ReferenceEquals(_firstType, typeContext))
        {
            return _firstSelections!;
        }

        if (ReferenceEquals(_secondType, typeContext))
        {
            return _secondSelections!;
        }

        return Processing.SelectionSet.Empty;
    }

    public void AddSelectionSet(
        IObjectType typeContext,
        IReadOnlyList<ISelection> selections,
        IReadOnlyList<IFragment>? fragments,
        bool isConditional)
    {
        var selectionSet = new SelectionSet(selections, fragments, isConditional);

        if (_map is { })
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

    public void ReplaceSelectionSet(IObjectType typeContext, ISelectionSet selectionSet)
    {
        if (_map is not null)
        {
            _map[typeContext] = selectionSet;
        }

        if (ReferenceEquals(_firstType, typeContext))
        {
            _firstSelections = selectionSet;
        }

        if (ReferenceEquals(_secondType, typeContext))
        {
            _secondSelections = selectionSet;
        }
    }
}
