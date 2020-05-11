using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution.Utilities
{
    internal sealed class PreparedSelectionSet : IPreparedSelectionSet
    {
        private static IPreparedSelectionList _empty = 
            new PreparedSelectionList(new IPreparedSelection[0], true);
        private ObjectType? _firstType;
        private IPreparedSelectionList? _firstSelections;
        private ObjectType? _secondType;
        private IPreparedSelectionList? _secondSelections;
        private Dictionary<ObjectType, IPreparedSelectionList>? _map;

        public PreparedSelectionSet(SelectionSetNode selectionSet)
        {
            SelectionSet = selectionSet;
        }

        public SelectionSetNode SelectionSet { get; }

        public IEnumerable<ObjectType> GetPossibleTypes()
        {
            if (_map is { })
            {
                foreach (ObjectType possibleType in _map.Keys)
                {
                    yield return possibleType;
                }
            }
            else
            {
                yield return _firstType!;

                if (_secondType is { })
                {
                    yield return _secondType;
                }
            }
        }

        public IPreparedSelectionList GetSelections(ObjectType typeContext)
        {
            if (_map is { })
            {
                if (_map.TryGetValue(typeContext, out IPreparedSelectionList? selections))
                {
                    return selections;
                }
                return _empty;
            }

            if (ReferenceEquals(_firstType, typeContext))
            {
                return _firstSelections!;
            }

            if (ReferenceEquals(_secondType, typeContext))
            {
                return _secondSelections!;
            }

            return _empty;
        }

        public void AddSelections(ObjectType typeContext, IPreparedSelectionList selections)
        {
            if (_map is { })
            {
                _map[typeContext] = selections;
            }
            else
            {
                if (_firstType is null)
                {
                    _firstType = typeContext;
                    _firstSelections = selections;
                }
                else if (_secondType is null)
                {
                    _secondType = typeContext;
                    _secondSelections = selections;
                }
                else
                {
                    _map = new Dictionary<ObjectType, IPreparedSelectionList>
                    {
                        { _firstType, _firstSelections! },
                        { _secondType, _secondSelections! },
                        { typeContext, selections }
                    };

                    _firstType = null;
                    _firstSelections = null;
                    _secondType = null;
                    _secondSelections = null;
                }
            }
        }
    }
}
